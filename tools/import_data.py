"""Подготовка данных заказчика (Приложение 2) к импорту в базу MebelOrg.

Читает xlsx во входной папке, нормализует грязные данные и генерирует db/data.sql.
Запуск: python tools/import_data.py
"""

import datetime
import os
import re

import openpyxl

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMPORT_DIR = os.path.join(BASE_DIR, "files", "import", "import")
OUT_FILE = os.path.join(BASE_DIR, "db", "data.sql")


def clean(value):
    """Нормализация текста: NBSP -> пробел, схлопывание пробелов, обрезка."""
    if value is None:
        return ""
    text = str(value).replace("\u00a0", " ")
    text = re.sub(r"\s+", " ", text)
    return text.strip()


def quote(value):
    if value is None:
        return "NULL"
    return "N'" + str(value).replace("'", "''") + "'"


def parse_date(value):
    """Дата из ячейки: datetime, строка 'дд.мм.гггг' или ISO. Некорректный день обрезается до конца месяца."""
    if value is None:
        return None
    if isinstance(value, datetime.datetime):
        return value.date()
    if isinstance(value, datetime.date):
        return value
    text = clean(value)
    for fmt in ("%d.%m.%Y", "%Y-%m-%d"):
        try:
            return datetime.datetime.strptime(text, fmt).date()
        except ValueError:
            continue
    match = re.match(r"^(\d{1,2})\.(\d{1,2})\.(\d{4})$", text)
    if match:
        day, month, year = (int(part) for part in match.groups())
        # 30.02.2024 -> последний день месяца (2024 високосный -> 29.02)
        next_month = datetime.date(year + (month == 12), month % 12 + 1, 1)
        last_day = (next_month - datetime.timedelta(days=1)).day
        return datetime.date(year, month, min(day, last_day))
    raise ValueError("Не удалось разобрать дату: " + text)


def read_rows(filename):
    path = os.path.join(IMPORT_DIR, filename)
    workbook = openpyxl.load_workbook(path, data_only=True)
    sheet = workbook.worksheets[0]
    return [row for row in sheet.iter_rows(values_only=True)]


class Registry:
    """Справочник «имя -> Id» с сохранением порядка появления."""

    def __init__(self):
        self.items = []

    def add(self, name):
        name = clean(name)
        if name and name not in self.items:
            self.items.append(name)
        return self.items.index(name) + 1

    def id_of(self, name):
        return self.items.index(clean(name)) + 1


def build_products():
    rows = read_rows("Tovar.xlsx")
    header = [clean(cell) for cell in rows[0]]
    index = {name: position for position, name in enumerate(header)}

    units = Registry()
    categories = Registry()
    manufacturers = Registry()
    suppliers = Registry()
    products = []

    for row in rows[1:]:
        if all(cell is None or clean(cell) == "" for cell in row):
            continue
        get = lambda column: row[index[column]] if index[column] < len(row) else None
        photo = clean(get("Фото")).lower() or "picture.png"
        products.append(
            {
                "article": clean(get("Артикул")),
                "name": clean(get("Наименование товара")),
                "unit_id": units.add(get("Единица измерения")),
                "price": float(get("Цена") or 0),
                "supplier_id": suppliers.add(get("Поставщик")),
                "manufacturer_id": manufacturers.add(get("Производитель")),
                "category_id": categories.add(get("Категория товара")),
                "discount": float(get("Действующая скидка") or 0),
                "quantity": int(get("Кол-во на складе") or 0),
                "description": clean(get("Описание товара")) or None,
                "image_path": photo,
            }
        )
    return units, categories, manufacturers, suppliers, products


def build_users():
    rows = read_rows("user_import.xlsx")
    roles = Registry()
    users = []
    for row in rows[1:]:
        if all(cell is None or clean(cell) == "" for cell in row):
            continue
        users.append(
            {
                "role_id": roles.add(row[0]),
                "full_name": clean(row[1]),
                "login": clean(row[2]),
                "password": clean(row[3]),
            }
        )
    return roles, users


def build_pickup_points():
    rows = read_rows("Пункты выдачи_import.xlsx")
    points = []
    for row in rows:
        if row[0] is None or clean(row[0]) == "":
            continue
        address = clean(row[0])
        address = address.replace("г.Лесной", "г. Лесной")
        address = re.sub(r"\s*,", ",", address)
        address = re.sub(r"\s+", " ", address).strip()
        points.append(address)
    return points


def build_orders(user_names, point_count, article_to_product):
    rows = read_rows("Заказ_import.xlsx")
    statuses = Registry()
    orders = []
    for row in rows[1:]:
        if row[0] is None or clean(row[0]) == "":
            continue
        composition = clean(row[1])
        parts = [clean(part) for part in composition.split(",")]
        items = []
        for position in range(0, len(parts) - 1, 2):
            article = parts[position]
            quantity = int(parts[position + 1] or 0)
            if article:
                items.append({"article": article, "quantity": quantity})

        client = clean(row[5])
        orders.append(
            {
                "number": int(row[0]),
                "items": items,
                "order_date": parse_date(row[2]),
                "delivery_date": parse_date(row[3]),
                "pickup_point_id": int(row[4]),
                "user_id": user_names.index(client) + 1 if client in user_names else 1,
                "code": int(row[6]),
                "status_id": statuses.add(row[7]),
            }
        )

    for order in orders:
        for item in order["items"]:
            if item["article"] not in article_to_product:
                raise ValueError("Неизвестный артикул в заказе: " + item["article"])
    # проверка, что пункт выдачи существует
    for order in orders:
        if order["pickup_point_id"] < 1 or order["pickup_point_id"] > point_count:
            raise ValueError("Неизвестный пункт выдачи: %d" % order["pickup_point_id"])
    return statuses, orders


def sql_insert(table, columns, rows, format_row):
    # Id объявлены как IDENTITY, поэтому явные значения вставляем через IDENTITY_INSERT
    lines = ["SET IDENTITY_INSERT %s ON;" % table]
    lines.append("INSERT INTO %s (%s) VALUES" % (table, ", ".join(columns)))
    values = []
    for row in rows:
        values.append("    (" + format_row(row) + ")")
    lines.append(",\n".join(values) + ";")
    lines.append("GO")
    lines.append("SET IDENTITY_INSERT %s OFF;" % table)
    lines.append("GO")
    lines.append("")
    return lines


def main():
    units, categories, manufacturers, suppliers, products = build_products()
    roles, users = build_users()
    points = build_pickup_points()
    article_to_product = {product["article"]: product for product in products}
    user_names = [user["full_name"] for user in users]
    statuses, orders = build_orders(user_names, len(points), article_to_product)

    out = []
    out.append("/* Данные ООО «МебельОрг». Сгенерировано tools/import_data.py. */")
    out.append("USE MebelOrg;")
    out.append("GO")
    out.append("")

    out.extend(sql_insert("Roles", ["Id", "Name"],
                          list(enumerate(roles.items, start=1)),
                          lambda item: "%d, %s" % (item[0], quote(item[1]))))
    out.extend(sql_insert("Units", ["Id", "Name"],
                          list(enumerate(units.items, start=1)),
                          lambda item: "%d, %s" % (item[0], quote(item[1]))))
    out.extend(sql_insert("Categories", ["Id", "Name"],
                          list(enumerate(categories.items, start=1)),
                          lambda item: "%d, %s" % (item[0], quote(item[1]))))
    out.extend(sql_insert("Manufacturers", ["Id", "Name"],
                          list(enumerate(manufacturers.items, start=1)),
                          lambda item: "%d, %s" % (item[0], quote(item[1]))))
    out.extend(sql_insert("Suppliers", ["Id", "Name"],
                          list(enumerate(suppliers.items, start=1)),
                          lambda item: "%d, %s" % (item[0], quote(item[1]))))
    out.extend(sql_insert("Statuses", ["Id", "Name"],
                          list(enumerate(statuses.items, start=1)),
                          lambda item: "%d, %s" % (item[0], quote(item[1]))))
    out.extend(sql_insert("PickupPoints", ["Id", "Address"],
                          list(enumerate(points, start=1)),
                          lambda item: "%d, %s" % (item[0], quote(item[1]))))

    out.extend(sql_insert(
        "Users", ["Id", "RoleId", "FullName", "Login", "Password"],
        list(enumerate(users, start=1)),
        lambda item: "%d, %d, %s, %s, %s" % (item[0], item[1]["role_id"], quote(item[1]["full_name"]),
                                             quote(item[1]["login"]), quote(item[1]["password"]))))

    def product_row(item):
        product = item[1]
        return "%d, %s, %s, %s, %d, %d, %d, %d, %.2f, %d, %.2f, %s" % (
            item[0],
            quote(product["article"]),
            quote(product["name"]),
            quote(product["description"]) if product["description"] else "NULL",
            product["category_id"],
            product["manufacturer_id"],
            product["supplier_id"],
            product["unit_id"],
            product["price"],
            product["quantity"],
            product["discount"],
            quote(product["image_path"]),
        )

    out.extend(sql_insert("Products",
                          ["Id", "Article", "Name", "Description", "CategoryId", "ManufacturerId",
                           "SupplierId", "UnitId", "Price", "Quantity", "Discount", "ImagePath"],
                          list(enumerate(products, start=1)),
                          product_row))

    out.extend(sql_insert(
        "Orders", ["Id", "Number", "UserId", "PickupPointId", "StatusId", "OrderDate", "DeliveryDate", "Code"],
        list(enumerate(orders, start=1)),
        lambda item: "%d, %d, %d, %d, %d, '%s', %s, %d" % (
            item[0],
            item[1]["number"],
            item[1]["user_id"],
            item[1]["pickup_point_id"],
            item[1]["status_id"],
            item[1]["order_date"].isoformat(),
            "'%s'" % item[1]["delivery_date"].isoformat() if item[1]["delivery_date"] else "NULL",
            item[1]["code"],
        )))

    item_rows = []
    product_by_article = {product["article"]: position for position, product in enumerate(products, start=1)}
    for order_position, order in enumerate(orders, start=1):
        for item in order["items"]:
            item_rows.append({"order_id": order_position,
                              "product_id": product_by_article[item["article"]],
                              "quantity": item["quantity"]})

    out.extend(sql_insert("OrderItems", ["Id", "OrderId", "ProductId", "Quantity"],
                          list(enumerate(item_rows, start=1)),
                          lambda item: "%d, %d, %d, %d" % (item[0], item[1]["order_id"],
                                                           item[1]["product_id"], item[1]["quantity"])))

    text = "\r\n".join(out)
    with open(OUT_FILE, "w", encoding="utf-8-sig", newline="") as stream:
        stream.write(text)
    print("Готово: %s" % OUT_FILE)
    print("Roles=%d Units=%d Categories=%d Manufacturers=%d Suppliers=%d Statuses=%d"
          % (len(roles.items), len(units.items), len(categories.items),
             len(manufacturers.items), len(suppliers.items), len(statuses.items)))
    print("PickupPoints=%d Users=%d Products=%d Orders=%d OrderItems=%d"
          % (len(points), len(users), len(products), len(orders), len(item_rows)))


if __name__ == "__main__":
    main()
