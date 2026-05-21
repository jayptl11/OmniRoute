from __future__ import annotations

import datetime as dt
import hashlib
import json
import random
import textwrap
import unicodedata
import uuid
from dataclasses import dataclass
from pathlib import Path


OUTPUT_PATH = Path(__file__).with_name("seed-demo-data.sql")
SEED_KEY = "omniroute-demo-seed-20260518"
PASSWORD_HASH = "$2b$12$KXghxFHChHpNo.aPMw9Qtu6HqBgFBVjzLcFD4SgiQ.726BjmlszLm"
NOW_UTC = dt.datetime(2026, 5, 18, 9, 0, 0)
RNG = random.Random(20260518)


class SqlExpr(str):
    pass


@dataclass(frozen=True)
class StoreInfo:
    code: str
    name: str
    address: str
    region: str
    is_new: bool = False


@dataclass(frozen=True)
class TeamInfo:
    name: str
    team_type: str
    leader_username: str
    store_code: str | None = None


def guid_for(key: str) -> str:
    digest = hashlib.md5(f"{SEED_KEY}:{key}".encode("utf-8")).digest()
    return str(uuid.UUID(bytes=digest)).upper()


def slugify(value: str) -> str:
    value = value.replace("Đ", "D").replace("đ", "d")
    normalized = unicodedata.normalize("NFKD", value)
    ascii_only = "".join(ch for ch in normalized if not unicodedata.combining(ch))
    cleaned = []
    for ch in ascii_only.lower():
        if ch.isalnum():
            cleaned.append(ch)
        else:
            cleaned.append(".")
    slug = "".join(cleaned)
    while ".." in slug:
        slug = slug.replace("..", ".")
    return slug.strip(".")


def sql_string(value: str | None) -> str:
    if value is None:
        return "NULL"
    return "N'" + value.replace("'", "''") + "'"


def sql_value(value) -> str:
    if isinstance(value, SqlExpr):
        return str(value)
    if value is None:
        return "NULL"
    if isinstance(value, bool):
        return "1" if value else "0"
    if isinstance(value, int):
        return str(value)
    if isinstance(value, str):
        return sql_string(value)
    raise TypeError(f"Unsupported SQL value: {type(value)!r}")


def offset_expr(minutes: int) -> SqlExpr:
    if minutes == 0:
        return SqlExpr("@Now")
    return SqlExpr(f"DATEADD(MINUTE, {minutes}, @Now)")


def date_literal(value: dt.date | None) -> SqlExpr | None:
    if value is None:
        return None
    return SqlExpr(f"'{value.isoformat()}'")


def expr_minutes(value: SqlExpr | None) -> int | None:
    if value is None:
        return None
    text = str(value).strip()
    if text == "@Now":
        return 0
    prefix = "DATEADD(MINUTE,"
    suffix = ", @Now)"
    if not (text.startswith(prefix) and text.endswith(suffix)):
        raise ValueError(f"Unsupported expression format: {text}")
    return int(text[len(prefix):-len(suffix)].strip())


def emit_table(
    table_name: str,
    declarations: list[tuple[str, str]],
    rows: list[dict],
) -> str:
    lines = [f"DECLARE @{table_name} TABLE ("]
    for idx, (column_name, column_type) in enumerate(declarations):
        comma = "," if idx < len(declarations) - 1 else ""
        lines.append(f"    [{column_name}] {column_type}{comma}")
    lines.append(");")
    lines.append("")
    if not rows:
        return "\n".join(lines)

    column_names = [name for name, _ in declarations]
    column_list = ", ".join(f"[{name}]" for name in column_names)
    lines.append(f"INSERT INTO @{table_name} ({column_list})")
    lines.append("VALUES")
    value_lines = []
    for row_idx, row in enumerate(rows):
        row_values = ", ".join(sql_value(row[column]) for column in column_names)
        suffix = "," if row_idx < len(rows) - 1 else ";"
        value_lines.append(f"    ({row_values}){suffix}")
    lines.extend(value_lines)
    lines.append("")
    return "\n".join(lines)


def pick_unique_names(count: int, start_offset: int = 0) -> list[tuple[str, str, str]]:
    surnames = [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
        "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Trương", "Đinh", "Mai", "Tạ",
    ]
    middles = [
        "Minh", "Thanh", "Ngọc", "Thu", "Hải", "Anh", "Quang", "Đức", "Bảo", "Phương",
        "Khánh", "Gia", "Hoài", "Tuấn", "Thảo", "Tú", "Xuân", "Kim", "Thành", "Lan",
    ]
    givens = [
        "An", "Bình", "Chi", "Dung", "Giang", "Hà", "Hải", "Hân", "Hiếu", "Hòa",
        "Hùng", "Khánh", "Khoa", "Lam", "Lan", "Linh", "Long", "Mai", "My", "Nam",
        "Ngân", "Nga", "Ngọc", "Nhung", "Phát", "Phong", "Phúc", "Quân", "Quỳnh", "Sơn",
        "Tâm", "Thảo", "Thành", "Thư", "Thủy", "Tiên", "Trang", "Trâm", "Trí", "Trinh",
        "Trúc", "Tú", "Tùng", "Uyên", "Vân", "Vi", "Việt", "Vy",
    ]
    combos = [(surname, middle, given) for surname in surnames for middle in middles for given in givens]
    RNG.shuffle(combos)
    selected = combos[start_offset:start_offset + count]
    if len(selected) != count:
        raise ValueError("Not enough generated names.")
    return selected


def profile_bio(role_name: str, store_name: str | None, team_name: str | None) -> str:
    if role_name == "QL":
        return f"Quản lý vận hành {store_name}"
    if role_name == "SS":
        return f"Phụ trách khách tại cửa hàng {store_name}"
    if role_name == "SA":
        return f"Tư vấn bán hàng thuộc {team_name}"
    if role_name == "CS":
        return f"Chăm sóc khách hàng thuộc {team_name}"
    if role_name == "DP":
        return f"Điều phối khách hàng thuộc {team_name}"
    if role_name == "TN":
        return f"Trưởng nhóm {team_name}"
    if role_name == "TV":
        return "Tư vấn tiếp nhận khách hàng đa kênh"
    return "Nhân sự OmniRoute"


def phone_sequence(prefixes: list[str], start_serial: int, count: int) -> list[str]:
    numbers: list[str] = []
    for idx in range(count):
        prefix = prefixes[idx % len(prefixes)]
        serial = start_serial + idx
        numbers.append(f"{prefix}{serial:07d}")
    return numbers


EXISTING_STORES = [
    StoreInfo("AGG01", "AGG01 - AGG, P, 60 Nguyễn Thái Học, Long Xuyên", "60 Nguyễn Thái Học, Phường Long Xuyên, Tỉnh An Giang", "An Giang"),
    StoreInfo("AGG02", "AGG20 - AGG, P, 281 Trần Hưng Đạo, Long Xuyên", "281 Trần Hưng Đạo, Phường Long Xuyên, Tỉnh An Giang", "An Giang"),
    StoreInfo("CTO01", "CTO13-CTO, W, 184A-184B-184D đường 30/4", "Số 184A 184B 184D, đường 30/4, Phường Ninh Kiều, Thành phố Cần Thơ", "Cần Thơ"),
    StoreInfo("CTO02", "CTO03-CTO, W, 23 Cách Mạng Tháng 8", "23 Cách Mạng Tháng 8, Phường Bình Thủy, Thành phố Cần Thơ", "Cần Thơ"),
    StoreInfo("CTO03", "STG15 - STG, X, 91 Hùng Vương, Sóc Trăng", "Số 91 Hùng Vương, Phường Sóc Trăng, Thành phố Cần Thơ", "Cần Thơ"),
    StoreInfo("DNG01", "QNM21-QNM, P, 211 Lý Thường Kiệt, Hội An", "211 Lý Thường Kiệt, Phường Hội An, Thành phố Đà Nẵng", "Đà Nẵng"),
    StoreInfo("DNG02", "DNG15-DNG, W, 832-834 Tôn Đức Thắng", "832 - 834 Tôn Đức Thắng, Tổ dân phố 1, Phường Hòa Khánh, Thành phố Đà Nẵng", "Đà Nẵng"),
    StoreInfo("DNG03", "QNM22 – QNM, P, 29 Phan Chu Trinh, Tam Kỳ", "29 Phan Chu Trinh, Phường Tam Kỳ, Thành phố Đà Nẵng", "Đà Nẵng"),
    StoreInfo("DNI01", "BPC02 - BPC, H, Phước Bình 01, Phước Long", "Số 234, Khu phố 02, Phường Phước Bình, Tỉnh Đồng Nai", "Đồng Nai"),
    StoreInfo("DNI02", "DNI02 - ĐNI, X, 92 Trần Phú, Long Khánh", "92 Trần Phú, Phường Long Khánh, Tỉnh Đồng Nai", "Đồng Nai"),
    StoreInfo("DNI03", "BPC01 - BPC, X, 973 Phú Riềng Đỏ, Đồng Xoài", "973 Phú Riềng Đỏ, Khu phố Tân Bình, Phường Bình Phước, Tỉnh Đồng Nai", "Đồng Nai"),
    StoreInfo("DNI04", "DNI03 - ĐNI, P, 240QL15 Phan Văn Thuận, Biên Hòa", "240-240A đường Phạm Văn Thuận, Phường Trấn Biên, Tỉnh Đồng Nai", "Đồng Nai"),
    StoreInfo("HNI01", "HNI05 - HNI, W, 26 Hàng Dầu", "Số 26 Hàng Dầu, Phường Hoàn Kiếm, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HNI02", "HNI02 - HNI, W, 26 Quang Trung", "Số 67 Quang Trung, Phường Hà Đông, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HNI03", "HNI09 - HNI, W, 145 Thái Hà", "145 Thái Hà, Phường Đống Đa, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HNI04", "HNI10 - HNI, W, 514 Nguyễn Trãi", "514 Nguyễn Trãi, Phường Thanh Xuân, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HNI05", "HNI04 - HNI, H, Sơn Lộc 01, Sơn Tây", "404 Chùa Thông, Phường Sơn Tây, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HNI06", "HNI16 - HNI, H, Trâu Quỳ 01, Gia Lâm", "199 Nguyễn Đức Thuận, Xã Gia Lâm, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HNI07", "HNI17 - HNI, H, Trạm Trôi 01, Hoài Đức", "Khu 7 Trạm Trôi, Xã Hoài Đức, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HNI08", "HNI15 - HNI, H, Đông Anh 01, Đông Anh", "Số 47A, 47B, 49, Tổ dân phố 04, Xã Đông Anh, Thành phố Hà Nội", "Hà Nội"),
    StoreInfo("HPG01", "HPG02 - HPG, W, 62 Lạch Tray", "62 Lạch Tray, Tổ dân phố Lạch Tray, Phường Gia Viên, Thành phố Hải Phòng", "Hải Phòng"),
    StoreInfo("HPG02", "HPG04 - HPG, H, Núi Đèo 01, Thủy Nguyên", "Ngã Tư Núi Đèo, Tổ dân phố Bạch Đằng, Phường Thủy Nguyên, Thành phố Hải Phòng", "Hải Phòng"),
    StoreInfo("HCM01", "BDG01 - BDG, P, 453 Đại Lộ Bình Dương, Thủ Dầu Một", "Số 453 Đại Lộ Bình Dương, Khu phố 01, Phường Thủ Dầu Một, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM02", "VTU02 - VTU, X, 224 Nguyễn Thanh Đằng, Bà Rịa", "224-226 Nguyễn Thanh Đằng, Phường Bà Rịa, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM03", "HCM02 - HCM, W, 715 Kha Vạn Cân", "715 Kha Vạn Cân, Khu phố 55, Phường Linh Xuân, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM04", "VTU03 - VTU, P, 353 Trương Công Định, Vũng Tàu", "353 Trương Công Định, Phường Tam Thắng, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM05", "HCM13 - HCM, W, Xô Viết Nghệ Tĩnh", "304-306 Xô Viết Nghệ Tĩnh, Phường Thạnh Mỹ Tây, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM06", "BDG02 - BDG, X, 1/97 Hòa Lân, Thuận An", "Số 1/97 Hòa Lân 2, Phường Thuận Giao, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM07", "HCM73-HCM, W, 20 Đường 3/2", "Số 20 đường 3 tháng 2, Phường Hòa Hưng, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM08", "BDG12 - BDG, P, 513 Phú Lợi, Thủ Dầu Một", "Số 513 đường Phú Lợi, Khu phố 08, Phường Phú Lợi, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM09", "HCM86 - HCM, W, 245 Nguyễn Thị Định", "245 Nguyễn Thị Định, Phường Bình Trưng, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("HCM10", "HCM88 - HCM, H, Củ Chi 01, Củ Chi", "883 Quốc lộ 22, Xã Tân An Hội, Thành phố Hồ Chí Minh", "Hồ Chí Minh"),
    StoreInfo("NAN01", "NAN01 - NAN, P, 16 Minh Khai, Vinh", "16 Nguyễn Thị Minh Khai, Tổ dân phố Khối Liên Cơ, Phường Thành Vinh, Tỉnh Nghệ An", "Nghệ An"),
    StoreInfo("NAN02", "NAN04 - NAN, H, Thái Hòa 01, Nghĩa Đàn", "Số 123 đường Nguyễn Trãi, Tổ dân phố Khối Kim Tân, Phường Thái Hòa, Tỉnh Nghệ An", "Nghệ An"),
]

NEW_STORES = [
    StoreInfo("HUE81", "HUE81 - Huế, Kim Long", "12 Nguyễn Phúc Chu, Phường Kim Long, Thành phố Huế", "Huế", True),
    StoreInfo("HUE82", "HUE82 - Huế, An Cựu", "88 Hùng Vương, Phường An Cựu, Thành phố Huế", "Huế", True),
    StoreInfo("QNI81", "QNI81 - Quảng Ngãi, Trần Hưng Đạo", "95 Trần Hưng Đạo, Phường Chánh Lộ, Tỉnh Quảng Ngãi", "Quảng Ngãi", True),
    StoreInfo("QNI82", "QNI82 - Quảng Ngãi, Nguyễn Nghiêm", "41 Nguyễn Nghiêm, Phường Nghĩa Chánh, Tỉnh Quảng Ngãi", "Quảng Ngãi", True),
    StoreInfo("LCI81", "LCI81 - Lào Cai, Bắc Cường", "66 Hoàng Liên, Phường Bắc Cường, Tỉnh Lào Cai", "Lào Cai", True),
    StoreInfo("LCI82", "LCI82 - Sa Pa, Xuân Viên", "27 Xuân Viên, Phường Sa Pa, Tỉnh Lào Cai", "Lào Cai", True),
    StoreInfo("TNH81", "TNH81 - Tây Ninh, Hòa Thành", "122 Phạm Hùng, Phường Long Hoa, Tỉnh Tây Ninh", "Tây Ninh", True),
    StoreInfo("TNH82", "TNH82 - Tây Ninh, Gò Dầu", "58 Quốc lộ 22B, Xã Gò Dầu, Tỉnh Tây Ninh", "Tây Ninh", True),
]

ACTIVE_STORES = EXISTING_STORES + NEW_STORES
STORE_BY_CODE = {store.code: store for store in ACTIVE_STORES}

NORTH_STORE_CODES = {"HNI01", "HNI02", "HNI03", "HNI04", "HNI05", "HNI06", "HNI07", "HNI08", "HPG01", "HPG02", "NAN01", "NAN02", "LCI81", "LCI82"}
SOUTH_STORE_CODES = {store.code for store in ACTIVE_STORES if store.code not in NORTH_STORE_CODES}

TEAM_LAYOUT = [
    ("sale-mien-bac", TeamInfo("Sale Miền Bắc", "Sale", "nguyen.huu.quang-tn-sale-bac", None)),
    ("sale-mien-nam", TeamInfo("Sale Miền Nam", "Sale", "tran.thanh.ha-tn-sale-nam", None)),
    ("cskh-mien-bac", TeamInfo("CSKH Miền Bắc", "Cskh", "le.ngoc.mai-tn-cskh-bac", None)),
    ("cskh-mien-nam", TeamInfo("CSKH Miền Nam", "Cskh", "pham.thu.hang-tn-cskh-nam", None)),
    ("dp-mien-bac", TeamInfo("Điều phối Miền Bắc", "StoreSupport", "vo.minh.khoi-tn-dp-bac", None)),
    ("dp-mien-nam", TeamInfo("Điều phối Miền Nam", "StoreSupport", "dang.hong.son-tn-dp-nam", None)),
]


def build_users() -> tuple[list[dict], list[dict], list[dict]]:
    user_rows: list[dict] = []
    profile_rows: list[dict] = []
    names = pick_unique_names(1200)
    name_index = 0
    used_usernames: set[str] = set()

    user_phones = phone_sequence(["098", "097", "096", "093", "091"], 1000000, 208)
    phone_index = 0

    def next_name() -> tuple[str, str, str]:
        nonlocal name_index
        value = names[name_index]
        name_index += 1
        return value

    def add_user(
        role_name: str,
        username: str,
        first_name: str,
        last_name: str,
        store_code: str | None,
        team_name: str | None,
        workload: int,
        created_days_ago: int,
        last_assigned_minutes_ago: int | None,
    ) -> None:
        nonlocal phone_index
        if username in used_usernames:
            raise ValueError(f"Duplicate username: {username}")
        used_usernames.add(username)

        user_id = guid_for(f"user:{username}")
        email = f"{username}@omniroute.example"
        store = STORE_BY_CODE.get(store_code) if store_code else None
        team_display = team_name
        phone = user_phones[phone_index]
        phone_index += 1
        birth_year = 1980 + (phone_index % 18)
        birth_month = 1 + (phone_index % 12)
        birth_day = 1 + (phone_index % 27)

        user_rows.append({
            "UserId": user_id,
            "Username": username,
            "Email": email,
            "PasswordHash": PASSWORD_HASH,
            "FirstName": first_name,
            "LastName": last_name,
            "CreatedAt": offset_expr(-(created_days_ago * 1440 + (phone_index % 300))),
            "LastLogin": offset_expr(-((phone_index % 17) * 60 + 45)),
            "RoleName": role_name,
            "TeamName": team_display,
            "StoreCode": store_code,
            "CurrentWorkload": workload,
            "IsActive": True,
            "ForcePasswordChange": False,
            "LastAssignedAt": offset_expr(-last_assigned_minutes_ago) if last_assigned_minutes_ago is not None else None,
        })

        profile_rows.append({
            "ProfileId": guid_for(f"profile:{username}"),
            "Username": username,
            "Bio": profile_bio(role_name, store.name if store else None, team_display),
            "AvatarUrl": None,
            "DateOfBirth": date_literal(dt.date(birth_year, birth_month, birth_day)),
            "Phone": phone,
            "Address": store.address if store else ("Hà Nội" if "Bắc" in (team_display or "") else "Hồ Chí Minh"),
            "UpdatedAt": offset_expr(-((phone_index % 21) * 90 + 30)),
        })

    # Team leaders with explicit usernames from the plan.
    fixed_tn = [
        ("nguyen.huu.quang-tn-sale-bac", "Nguyễn", "Hữu Quang", "Sale Miền Bắc", "HNI01"),
        ("tran.thanh.ha-tn-sale-nam", "Trần", "Thanh Hà", "Sale Miền Nam", "HCM01"),
        ("le.ngoc.mai-tn-cskh-bac", "Lê", "Ngọc Mai", "CSKH Miền Bắc", "HPG01"),
        ("pham.thu.hang-tn-cskh-nam", "Phạm", "Thu Hằng", "CSKH Miền Nam", "CTO01"),
        ("vo.minh.khoi-tn-dp-bac", "Võ", "Minh Khôi", "Điều phối Miền Bắc", "NAN01"),
        ("dang.hong.son-tn-dp-nam", "Đặng", "Hồng Sơn", "Điều phối Miền Nam", "DNG01"),
    ]
    for idx, (username, first_name, last_name, team_name, store_code) in enumerate(fixed_tn, start=1):
        add_user("TN", username, first_name, last_name, store_code, team_name, idx % 3, 45 + idx, 90 + idx * 20)

    # TV users.
    tv_assignments = [("HNI02", None), ("HPG02", None), ("NAN02", None), ("HCM02", None), ("CTO02", None), ("DNG02", None)]
    for idx, (store_code, _) in enumerate(tv_assignments, start=1):
        surname, middle, given = next_name()
        username = f"{slugify(f'{surname} {middle} {given}')}-tv-{idx:02d}"
        add_user("TV", username, surname, f"{middle} {given}", store_code, None, idx % 2, 10 + idx, None)

    # SA users.
    sa_store_codes = ["HNI03", "HNI04", "HPG01", "NAN01", "LCI81", "HUE81", "HCM03", "HCM04", "DNI01", "DNG01", "CTO01", "AGG01"]
    for idx, store_code in enumerate(sa_store_codes, start=1):
        surname, middle, given = next_name()
        team_name = "Sale Miền Bắc" if store_code in NORTH_STORE_CODES else "Sale Miền Nam"
        username = f"{slugify(f'{surname} {middle} {given}')}-sa-{store_code.lower()}"
        add_user("SA", username, surname, f"{middle} {given}", store_code, team_name, 3 + (idx % 6), 20 + idx, 45 + idx * 12)

    # CS users.
    cs_store_codes = ["HNI05", "HNI06", "HPG02", "LCI82", "HCM05", "DNI02", "CTO03", "QNI81"]
    for idx, store_code in enumerate(cs_store_codes, start=1):
        surname, middle, given = next_name()
        team_name = "CSKH Miền Bắc" if store_code in NORTH_STORE_CODES else "CSKH Miền Nam"
        username = f"{slugify(f'{surname} {middle} {given}')}-cs-{store_code.lower()}"
        add_user("CS", username, surname, f"{middle} {given}", store_code, team_name, 2 + (idx % 5), 22 + idx, 60 + idx * 10)

    # DP users.
    dp_store_codes = ["HNI07", "HNI08", "NAN01", "LCI81", "HCM06", "DNG02", "TNH81", "QNI82"]
    for idx, store_code in enumerate(dp_store_codes, start=1):
        surname, middle, given = next_name()
        team_name = "Điều phối Miền Bắc" if store_code in NORTH_STORE_CODES else "Điều phối Miền Nam"
        username = f"{slugify(f'{surname} {middle} {given}')}-dp-{store_code.lower()}"
        add_user("DP", username, surname, f"{middle} {given}", store_code, team_name, 1 + (idx % 4), 18 + idx, 80 + idx * 15)

    manager_rows: list[dict] = []
    staff_rows: list[dict] = []
    for idx, store in enumerate(ACTIVE_STORES, start=1):
        surname, middle, given = next_name()
        manager_username = f"{slugify(f'{surname} {middle} {given}')}-ql-{store.code.lower()}"
        add_user("QL", manager_username, surname, f"{middle} {given}", store.code, None, idx % 2, 30 + idx, None)
        manager_rows.append({"StoreCode": store.code, "Username": manager_username})

        for slot in range(1, 4):
            surname, middle, given = next_name()
            staff_username = f"{slugify(f'{surname} {middle} {given}')}-ss-{store.code.lower()}-{slot}"
            add_user("SS", staff_username, surname, f"{middle} {given}", store.code, None, slot % 3, 15 + idx + slot, 30 + slot * 20)
            staff_rows.append({"StoreCode": store.code, "Username": staff_username})

    if len(user_rows) != 208:
        raise ValueError(f"Expected 208 users, got {len(user_rows)}")

    return user_rows, profile_rows, manager_rows


USER_ROWS, PROFILE_ROWS, MANAGER_ROWS = build_users()
USERS_BY_ROLE: dict[str, list[dict]] = {}
for row in USER_ROWS:
    USERS_BY_ROLE.setdefault(row["RoleName"], []).append(row)

TEAM_ROWS = [
    {
        "TeamId": guid_for(f"team:{team.name}"),
        "TeamName": team.name,
        "TeamType": team.team_type,
        "LeaderUsername": team.leader_username,
        "StoreCode": team.store_code,
        "IsActive": True,
        "CreatedAt": offset_expr(-(90 + idx * 60)),
    }
    for idx, (_, team) in enumerate(TEAM_LAYOUT, start=1)
]


MASTER_DATA_ROWS = [
    ("Product", "PRD_FIBER_HOME_150", "Gói internet cáp quang 150Mbps", "Gói cước gia đình tốc độ ổn định cho nhu cầu học tập và giải trí"),
    ("Product", "PRD_FIBER_HOME_300", "Gói internet cáp quang 300Mbps", "Gói cước gia đình tốc độ cao cho nhà nhiều thiết bị"),
    ("Product", "PRD_FIBER_BIZ_500", "Gói internet doanh nghiệp 500Mbps", "Kết nối tốc độ cao cho văn phòng vừa và nhỏ"),
    ("Product", "PRD_FIBER_BIZ_1G", "Gói internet doanh nghiệp 1Gbps", "Đường truyền ưu tiên cho trung tâm vận hành và tổng đài"),
    ("Product", "PRD_COMBO_TV_FAMILY", "Combo internet và truyền hình Gia đình", "Combo truyền hình cơ bản đi kèm internet cáp quang"),
    ("Product", "PRD_COMBO_TV_PREMIUM", "Combo internet và truyền hình Nâng cao", "Combo có thêm kho phim và kênh thể thao"),
    ("Product", "PRD_CAMERA_INDOOR", "Camera an ninh trong nhà", "Camera quay trong nhà, hỗ trợ quan sát qua điện thoại"),
    ("Product", "PRD_CAMERA_OUTDOOR", "Camera an ninh ngoài trời", "Camera ngoài trời chống nước, hồng ngoại ban đêm"),
    ("Product", "PRD_CAMERA_SHOP", "Gói camera cho cửa hàng 4 mắt", "Bộ camera phù hợp cửa hàng bán lẻ và quầy giao dịch"),
    ("Product", "PRD_MESH_WIFI_2PK", "Bộ mesh wifi 2 điểm", "Phù hợp căn hộ và nhà ống 2 tầng"),
    ("Product", "PRD_MESH_WIFI_3PK", "Bộ mesh wifi 3 điểm", "Phủ sóng toàn bộ nhà phố hoặc văn phòng nhỏ"),
    ("Product", "PRD_ROUTER_AX3000", "Router wifi AX3000", "Router wifi 6 cho hộ gia đình dùng nhiều thiết bị"),
    ("Product", "PRD_ROUTER_AX6000", "Router wifi AX6000", "Router hiệu năng cao cho quán cà phê và showroom"),
    ("Product", "PRD_SETTOP_ANDROID", "Đầu giải mã Android TV", "Đầu giải mã truyền hình thông minh hỗ trợ ứng dụng OTT"),
    ("Product", "PRD_SETTOP_KIDS", "Đầu giải mã Truyền hình thiếu nhi", "Bộ thiết bị dành cho gia đình có trẻ nhỏ"),
    ("Product", "PRD_SMARTHOME_SENSOR", "Bộ cảm biến nhà thông minh", "Cảm biến mở cửa, chuyển động và cảnh báo khói"),
    ("Product", "PRD_SMARTHOME_SOCKET", "Ổ cắm thông minh", "Điều khiển từ xa qua ứng dụng di động"),
    ("Product", "PRD_SMARTHOME_BELL", "Chuông cửa thông minh", "Chuông hình ghi hình và cảnh báo chuyển động"),
    ("Product", "PRD_IP_STATIC", "IP tĩnh doanh nghiệp", "Dịch vụ IP tĩnh phục vụ camera, VPN và máy chủ"),
    ("Product", "PRD_WIFI_MARKETING", "Gói wifi marketing cho cửa hàng", "Đăng nhập wifi kèm landing page khuyến mãi"),
    ("Product", "PRD_SUPPORT_PREMIUM", "Gói hỗ trợ kỹ thuật Premium", "Ưu tiên kỹ thuật viên và tổng đài doanh nghiệp"),
    ("Product", "PRD_BACKUP_LINK", "Đường truyền dự phòng", "Kết nối backup cho điểm giao dịch quan trọng"),
    ("Product", "PRD_INSTALL_FAST", "Lắp đặt nhanh trong 24 giờ", "Dịch vụ ưu tiên lịch hẹn và triển khai nhanh"),
    ("Product", "PRD_BUNDLE_CAMERA_NET", "Combo internet và camera", "Bán kèm internet cáp quang với bộ camera an ninh"),
    ("LostReason", "LOST_PRICE", "Giá chưa phù hợp", "Khách so sánh với đơn vị khác và thấy tổng chi phí cao hơn kỳ vọng"),
    ("LostReason", "LOST_COMPETITOR", "Đã chọn nhà cung cấp khác", "Khách ký trước với đối tác khác trước khi chốt"),
    ("LostReason", "LOST_NO_RESPONSE", "Khách không phản hồi", "Không liên lạc lại được sau nhiều lần hẹn"),
    ("LostReason", "LOST_NO_NEED", "Tạm thời chưa có nhu cầu", "Kế hoạch đầu tư của khách tạm dừng"),
    ("LostReason", "LOST_COVERAGE", "Khu vực chưa đáp ứng", "Khu vực lắp đặt chưa sẵn hạ tầng hoặc lịch kỹ thuật phù hợp"),
    ("LostReason", "LOST_TIMING", "Thời gian triển khai chưa kịp", "Khách cần lắp đặt gấp hơn thời gian cam kết"),
    ("LostReason", "LOST_BUDGET_CUT", "Ngân sách bị cắt giảm", "Bộ phận mua sắm giảm ngân sách trong kỳ"),
    ("LostReason", "LOST_FEATURE_GAP", "Thiếu tính năng khách cần", "Yêu cầu kỹ thuật chưa khớp với sản phẩm hiện có"),
    ("LostReason", "LOST_PROCUREMENT", "Quy trình mua sắm kéo dài", "Hồ sơ nội bộ của khách bị dừng hoặc đổi người duyệt"),
    ("LostReason", "LOST_DECISION_DELAY", "Người quyết định chưa chốt", "Người có thẩm quyền chưa phê duyệt hợp đồng"),
    ("LostReason", "LOST_STORE_FAR", "Khoảng cách cửa hàng chưa thuận tiện", "Khách muốn điểm hỗ trợ gần hơn nơi kinh doanh"),
    ("LostReason", "LOST_AFTER_TRIAL", "Sau dùng thử chưa phù hợp", "Khách dùng thử nhưng chưa thấy khác biệt rõ ràng"),
    ("CancelReason", "CANCEL_RESCHEDULE", "Khách hẹn lại vào đợt sau", "Khách muốn đóng case hiện tại và mở lại khi cần"),
    ("CancelReason", "CANCEL_DUPLICATE", "Trùng yêu cầu với hồ sơ khác", "Hồ sơ này trùng với yêu cầu đã có trước đó"),
    ("CancelReason", "CANCEL_WRONG_INFO", "Thông tin tiếp nhận chưa chính xác", "Số điện thoại hoặc nhu cầu ban đầu chưa đúng"),
    ("CancelReason", "CANCEL_OUT_OF_SCOPE", "Yêu cầu ngoài phạm vi hỗ trợ", "Nhu cầu không thuộc dịch vụ đang cung cấp"),
    ("CancelReason", "CANCEL_CUSTOMER_REQUEST", "Khách chủ động dừng yêu cầu", "Khách xác nhận không cần xử lý tiếp"),
    ("CancelReason", "CANCEL_CHANGE_OWNER", "Khách đổi đầu mối phụ trách", "Cần mở hồ sơ mới cho người liên hệ khác"),
    ("CancelReason", "CANCEL_DELAY_TOO_LONG", "Khách không muốn chờ thêm", "Khách đóng yêu cầu vì tiến độ không còn phù hợp"),
    ("CancelReason", "CANCEL_MOVED_LOCATION", "Khách đã chuyển địa điểm", "Địa chỉ mới cần tạo yêu cầu khác để xử lý"),
    ("CancelReason", "CANCEL_INTERNAL_NOTE", "Đóng theo chỉ đạo nội bộ", "Trường hợp đặc thù được lãnh đạo yêu cầu kết thúc"),
    ("CancelReason", "CANCEL_CONTACT_ERROR", "Không xác thực được người liên hệ", "Không đủ căn cứ xác nhận chính chủ yêu cầu"),
    ("CancelReason", "CANCEL_SERVICE_DONE", "Nhu cầu đã được xử lý nơi khác", "Khách cho biết bộ phận khác đã hoàn tất"),
    ("CancelReason", "CANCEL_STORE_PICKUP", "Khách chuyển sang làm trực tiếp tại cửa hàng", "Khách muốn được phục vụ trực tiếp tại điểm giao dịch"),
]

MASTER_DATA_SEED = [
    {
        "Id": guid_for(f"master:{category}:{code}"),
        "Category": category,
        "Code": code,
        "DisplayName": display_name,
        "Description": description,
        "SortOrder": idx,
        "IsActive": True,
        "CreatedAt": offset_expr(-(idx * 5)),
    }
    for idx, (category, code, display_name, description) in enumerate(MASTER_DATA_ROWS, start=1)
]

ROUTING_RULE_SEED = [
    {
        "Id": guid_for("rule:zalo-khieu-nai"),
        "RuleName": "Khiếu nại qua Zalo cần phản hồi sớm",
        "Description": "Ưu tiên các tin nhắn Zalo có từ khóa khiếu nại, mất kết nối hoặc phản ánh dịch vụ.",
        "PriorityOrder": 11,
        "ConditionChannelJson": json.dumps(["Zalo"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["khiếu nại", "phản ánh", "mất mạng", "không vào được", "bực mình"], ensure_ascii=False),
        "ActionGroup": "Cskh",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-110),
        "UpdatedAt": offset_expr(-110),
    },
    {
        "Id": guid_for("rule:email-ho-tro"),
        "RuleName": "Email hỗ trợ kỹ thuật và hóa đơn",
        "Description": "Các email có nội dung hỗ trợ kỹ thuật, hóa đơn, đối soát hoặc không xem được truyền hình chuyển vào CSKH.",
        "PriorityOrder": 12,
        "ConditionChannelJson": json.dumps(["Email"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["hóa đơn", "đối soát", "kỹ thuật", "không xem được", "mất tín hiệu"], ensure_ascii=False),
        "ActionGroup": "Cskh",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-112),
        "UpdatedAt": offset_expr(-112),
    },
    {
        "Id": guid_for("rule:referral-sale"),
        "RuleName": "Khách giới thiệu từ đối tác bán hàng",
        "Description": "Lead từ kênh giới thiệu với nhu cầu lắp mới hoặc mở điểm bán mới ưu tiên vào Sale.",
        "PriorityOrder": 13,
        "ConditionChannelJson": json.dumps(["Referral"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["giới thiệu", "đối tác", "mở mới", "chi nhánh", "showroom"], ensure_ascii=False),
        "ActionGroup": "Sale",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-114),
        "UpdatedAt": offset_expr(-114),
    },
    {
        "Id": guid_for("rule:email-bao-gia"),
        "RuleName": "Email xin báo giá và hợp đồng",
        "Description": "Các email yêu cầu báo giá, đề nghị hợp đồng hoặc khảo sát triển khai chuyển vào Sale.",
        "PriorityOrder": 14,
        "ConditionChannelJson": json.dumps(["Email"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["báo giá", "hợp đồng", "khảo sát", "đăng ký mới", "triển khai"], ensure_ascii=False),
        "ActionGroup": "Sale",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-116),
        "UpdatedAt": offset_expr(-116),
    },
    {
        "Id": guid_for("rule:zalo-bao-hanh"),
        "RuleName": "Bảo hành thiết bị qua Zalo",
        "Description": "Tin nhắn Zalo về lỗi thiết bị, camera hoặc đầu giải mã được chuyển thẳng vào CSKH.",
        "PriorityOrder": 15,
        "ConditionChannelJson": json.dumps(["Zalo"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["bảo hành", "camera", "đầu thu", "thiết bị", "không lên nguồn"], ensure_ascii=False),
        "ActionGroup": "Cskh",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-118),
        "UpdatedAt": offset_expr(-118),
    },
    {
        "Id": guid_for("rule:email-gia-han"),
        "RuleName": "Gia hạn và nâng cấp qua email",
        "Description": "Nhu cầu nâng băng thông, gia hạn hoặc đổi thiết bị từ email chuyển vào Sale.",
        "PriorityOrder": 16,
        "ConditionChannelJson": json.dumps(["Email"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["gia hạn", "nâng cấp", "đổi modem", "băng thông", "thêm camera"], ensure_ascii=False),
        "ActionGroup": "Sale",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-120),
        "UpdatedAt": offset_expr(-120),
    },
    {
        "Id": guid_for("rule:referral-store"),
        "RuleName": "Giới thiệu khách đến làm trực tiếp tại cửa hàng",
        "Description": "Khách được giới thiệu muốn tới điểm giao dịch hoặc ký hồ sơ trực tiếp chuyển vào điều phối cửa hàng.",
        "PriorityOrder": 17,
        "ConditionChannelJson": json.dumps(["Referral"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["đến cửa hàng", "ký hồ sơ", "làm trực tiếp", "gặp quản lý", "điểm giao dịch"], ensure_ascii=False),
        "ActionGroup": "StoreSupport",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-122),
        "UpdatedAt": offset_expr(-122),
    },
    {
        "Id": guid_for("rule:zalo-store-visit"),
        "RuleName": "Đặt lịch tới cửa hàng qua Zalo",
        "Description": "Khách nhắn Zalo để đặt lịch lấy thiết bị hoặc hoàn tất giấy tờ tại cửa hàng.",
        "PriorityOrder": 18,
        "ConditionChannelJson": json.dumps(["Zalo"], ensure_ascii=False),
        "ConditionKeywordsJson": json.dumps(["đặt lịch", "tới cửa hàng", "lấy thiết bị", "nhận sim", "ký giấy tờ"], ensure_ascii=False),
        "ActionGroup": "StoreSupport",
        "ActionTeamName": None,
        "IsActive": True,
        "CreatedAt": offset_expr(-124),
        "UpdatedAt": offset_expr(-124),
    },
]


def channel_score(channel: str) -> int:
    return {
        "Walkin": 30,
        "Hotline": 25,
        "Chat": 20,
        "Referral": 20,
        "Webform": 15,
        "Email": 10,
        "Zalo": 10,
    }[channel]


def need_score(need_type: str) -> int:
    return {
        "CskhComplaint": 30,
        "CskhWarranty": 25,
        "SaleNew": 20,
        "SaleUpgrade": 20,
        "CskhSupport": 15,
        "SaleRenew": 15,
        "StoreVisit": 10,
        "Other": 5,
    }[need_type]


def priority_level(score: int) -> str:
    if score >= 70:
        return "High"
    if score >= 40:
        return "Medium"
    return "Low"


def sla_hours(group_name: str, level: str) -> int:
    matrix = {
        "Sale": {"High": 2, "Medium": 4, "Low": 8},
        "Cskh": {"High": 1, "Medium": 4, "Low": 24},
        "StoreSupport": {"High": 4, "Medium": 8, "Low": 24},
    }
    return matrix[group_name][level]


PRODUCT_POOL = [item["DisplayName"] for item in MASTER_DATA_SEED if item["Category"] == "Product"]
LOST_REASON_POOL = [item["DisplayName"] for item in MASTER_DATA_SEED if item["Category"] == "LostReason"]
CANCEL_REASON_POOL = [item["DisplayName"] for item in MASTER_DATA_SEED if item["Category"] == "CancelReason"]


def build_need_description(need_type: str, channel: str, product_a: str, product_b: str, region: str, store_name: str) -> str:
    templates = {
        "SaleNew": [
            f"Khách muốn đăng ký mới {product_a.lower()} cho gia đình tại {region}, ưu tiên lắp trong tuần này.",
            f"Khách cần tư vấn {product_a.lower()} và hỏi thêm {product_b.lower()} cho cửa hàng gần {store_name}.",
            f"Khách để lại yêu cầu mua mới {product_a.lower()} qua {channel.lower()}, cần báo giá rõ chi phí lắp đặt.",
        ],
        "SaleUpgrade": [
            f"Khách đang dùng gói cũ và muốn nâng cấp lên {product_a.lower()} vì nhu cầu tăng thiết bị.",
            f"Khách hỏi đổi modem và nâng cấp đường truyền lên {product_a.lower()} cho văn phòng tại {region}.",
            f"Khách muốn thêm {product_b.lower()} kèm gói hiện tại để phủ sóng ổn định hơn.",
        ],
        "SaleRenew": [
            f"Khách muốn gia hạn hợp đồng dịch vụ và cân nhắc chuyển sang {product_a.lower()}.",
            f"Khách cần báo giá gia hạn 12 tháng, đồng thời hỏi chính sách giữ số và giữ thiết bị hiện tại.",
            f"Khách đề nghị làm lại phụ lục hợp đồng và gia hạn thêm dịch vụ {product_a.lower()}.",
        ],
        "CskhSupport": [
            f"Khách cần hỗ trợ cấu hình lại {product_a.lower()} vì wifi chập chờn ở khu vực {region}.",
            f"Khách báo không truy cập được một số thiết bị sau khi đổi mật khẩu mạng tại nhà.",
            f"Khách cần hướng dẫn kiểm tra kết nối và khởi động lại modem từ xa qua {channel.lower()}.",
        ],
        "CskhComplaint": [
            f"Khách phản ánh chất lượng đường truyền không ổn định và yêu cầu kiểm tra trong ngày.",
            f"Khách bức xúc vì lịch kỹ thuật thay đổi nhiều lần, đề nghị quản lý liên hệ xác nhận.",
            f"Khách phàn nàn hóa đơn và cước phát sinh chưa được giải thích rõ ràng.",
        ],
        "CskhWarranty": [
            f"Khách báo {product_a.lower()} lỗi nguồn, cần bảo hành hoặc đổi thiết bị sớm.",
            f"Khách phản ánh camera mất tín hiệu sau mưa lớn, muốn kỹ thuật kiểm tra tận nơi.",
            f"Khách cần xử lý bảo hành cho đầu giải mã vì khởi động lại liên tục.",
        ],
        "StoreVisit": [
            f"Khách muốn tới {store_name} để ký hồ sơ và nhận thiết bị trong ngày.",
            f"Khách đề nghị đặt lịch gặp quản lý cửa hàng tại {region} để xem trực tiếp thiết bị.",
            f"Khách muốn đến cửa hàng gần nhất để thanh toán và hoàn tất thủ tục lắp đặt.",
        ],
        "Other": [
            f"Khách hỏi thêm thông tin tổng quát về điểm giao dịch và giấy tờ cần mang theo.",
            f"Khách muốn được gọi lại để xác nhận khu vực phục vụ trước khi quyết định.",
            f"Khách nhờ hướng dẫn chuyển quyền sở hữu hợp đồng tại cửa hàng gần {region}.",
        ],
    }
    choices = templates[need_type]
    return choices[RNG.randrange(len(choices))]


def build_leads() -> list[dict]:
    sale_statuses = (["Assigned"] * 90 + ["Contacted"] * 110 + ["InProgress"] * 80 +
                     ["Won"] * 120 + ["Lost"] * 65 + ["Cancelled"] * 25 + ["PendingAssignment"] * 10)
    cskh_statuses = ["Assigned"] * 60 + ["Contacted"] * 55 + ["InProgress"] * 60 + ["PendingAssignment"] * 15 + ["Lost"] * 10 + ["Cancelled"] * 20
    store_statuses = ["PendingDispatch"] * 60 + ["Assigned"] * 45 + ["Contacted"] * 30 + ["InProgress"] * 20 + ["Lost"] * 10 + ["Cancelled"] * 15

    sale_need_types = ["SaleNew"] * 330 + ["SaleUpgrade"] * 80 + ["SaleRenew"] * 90
    cskh_need_types = ["CskhSupport"] * 140 + ["CskhComplaint"] * 45 + ["CskhWarranty"] * 35
    store_need_types = ["StoreVisit"] * 140 + ["Other"] * 40

    for lst in (sale_statuses, cskh_statuses, store_statuses, sale_need_types, cskh_need_types, store_need_types):
        RNG.shuffle(lst)

    channels = (["Walkin"] * 250 + ["Hotline"] * 180 + ["Chat"] * 160 + ["Webform"] * 130 +
                ["Zalo"] * 90 + ["Email"] * 55 + ["Referral"] * 35)
    RNG.shuffle(channels)

    users_tv = USERS_BY_ROLE["TV"]
    users_sa = USERS_BY_ROLE["SA"]
    users_cs = USERS_BY_ROLE["CS"]
    users_ss = USERS_BY_ROLE["SS"]
    ss_by_store: dict[str, list[str]] = {}
    for user in users_ss:
        ss_by_store.setdefault(user["StoreCode"], []).append(user["Username"])

    customer_names = pick_unique_names(900, start_offset=300)
    rows: list[dict] = []
    per_day_seq: dict[str, int] = {}
    assignable_store_codes = [store.code for store in ACTIVE_STORES]

    def next_lead_code(created_minutes_ago: int) -> str:
        created_date = (NOW_UTC + dt.timedelta(minutes=created_minutes_ago)).date()
        key = created_date.strftime("%Y%m%d")
        per_day_seq[key] = per_day_seq.get(key, 9000) + 1
        return f"LD-{key}-{per_day_seq[key]}"

    def status_window(group_name: str, status: str, index: int) -> int:
        if status in {"Won", "Lost", "Cancelled"}:
            days = 12 + (index % 78)
            hours = (index * 5) % 23
            mins = (index * 11) % 59
            return -(days * 1440 + hours * 60 + mins)
        if status == "PendingDispatch":
            days = index % 12
            hours = (index * 7) % 18
            mins = (index * 13) % 60
            return -(days * 1440 + hours * 60 + mins)
        if status == "PendingAssignment":
            days = index % 10
            hours = (index * 9) % 16
            mins = (index * 17) % 60
            return -(days * 1440 + hours * 60 + mins)
        if status == "Assigned":
            days = index % 7
            hours = (index * 3) % 15
            mins = (index * 19) % 60
            return -(days * 1440 + hours * 60 + mins)
        days = 2 + (index % 24)
        hours = (index * 4) % 21
        mins = (index * 23) % 60
        return -(days * 1440 + hours * 60 + mins)

    seed_specs = []
    for group_name, statuses, need_types in [
        ("Sale", sale_statuses, sale_need_types),
        ("Cskh", cskh_statuses, cskh_need_types),
        ("StoreSupport", store_statuses, store_need_types),
    ]:
        for idx, (status, need_type) in enumerate(zip(statuses, need_types), start=1):
            seed_specs.append({"Group": group_name, "Status": status, "NeedType": need_type, "Index": idx})
    RNG.shuffle(seed_specs)

    for global_index, spec in enumerate(seed_specs, start=1):
        surname, middle, given = customer_names[global_index - 1]
        full_name = f"{surname} {middle} {given}"
        channel = channels[global_index - 1]
        group_name = spec["Group"]
        status = spec["Status"]
        need_type = spec["NeedType"]

        store_code = None
        assigned_username = None
        if status not in {"PendingDispatch", "PendingAssignment"}:
            store_code = assignable_store_codes[(global_index - 1) % len(assignable_store_codes)]
            if group_name == "Sale":
                team_pool = [u["Username"] for u in users_sa if (u["StoreCode"] in NORTH_STORE_CODES) == (store_code in NORTH_STORE_CODES)]
                assigned_username = team_pool[(global_index - 1) % len(team_pool)]
            elif group_name == "Cskh":
                team_pool = [u["Username"] for u in users_cs if (u["StoreCode"] in NORTH_STORE_CODES) == (store_code in NORTH_STORE_CODES)]
                assigned_username = team_pool[(global_index - 1) % len(team_pool)]
            else:
                assigned_username = ss_by_store[store_code][(global_index - 1) % len(ss_by_store[store_code])]

        created_offset = status_window(group_name, status, global_index)
        assign_delay = 30 + (global_index % 180)
        assigned_offset = created_offset + assign_delay if assigned_username else None

        history_bonus = [0, 0, 0, 5, 0, 0, 15][global_index % 7]
        base_score = min(100, channel_score(channel) + need_score(need_type) + history_bonus)
        level = priority_level(base_score)
        violation = assigned_username is not None and ((global_index + len(status) + len(channel)) % 7 == 0)

        deadline_offset = None
        if assigned_offset is not None:
            deadline_offset = assigned_offset + sla_hours(group_name, level) * 60

        closed_offset = None
        if status in {"Won", "Lost", "Cancelled"}:
            processing_minutes = 300 + (global_index % 3200)
            if violation and deadline_offset is not None:
                closed_offset = deadline_offset + 60 + (global_index % 360)
            elif deadline_offset is not None:
                closed_offset = min(deadline_offset - 30, assigned_offset + processing_minutes)
            else:
                closed_offset = assigned_offset + processing_minutes

        if status in {"Assigned", "Contacted", "InProgress"} and not violation and deadline_offset is not None and deadline_offset < 0:
            shift = abs(deadline_offset) + 90
            created_offset += shift
            if assigned_offset is not None:
                assigned_offset += shift
            deadline_offset += shift

        if status == "PendingAssignment":
            violation = False

        created_by_username = users_tv[(global_index - 1) % len(users_tv)]["Username"]
        store = STORE_BY_CODE[store_code] if store_code else ACTIVE_STORES[(global_index - 1) % len(ACTIVE_STORES)]
        product_a = PRODUCT_POOL[(global_index - 1) % len(PRODUCT_POOL)]
        product_b = PRODUCT_POOL[(global_index + 7) % len(PRODUCT_POOL)]
        description = build_need_description(need_type, channel, product_a, product_b, store.region, store.name)

        interests = None
        if need_type in {"SaleNew", "SaleUpgrade", "SaleRenew"}:
            interests = json.dumps([product_a, product_b] if global_index % 3 == 0 else [product_a], ensure_ascii=False)
        elif need_type in {"CskhWarranty", "CskhSupport"} and global_index % 4 == 0:
            interests = json.dumps([product_a], ensure_ascii=False)

        email_local = slugify(f"{surname} {middle} {given}")
        lead_code = next_lead_code(created_offset)
        row = {
            "LeadId": guid_for(f"lead:{lead_code}"),
            "LeadCode": lead_code,
            "CustomerName": full_name,
            "CustomerPhone": phone_sequence(["090", "091", "092", "093", "094", "096", "097", "098"], 2000000, 900)[global_index - 1],
            "CustomerAddress": f"Số {(global_index % 97) + 3} {store.address}",
            "CustomerEmail": f"{email_local}.{global_index % 17 + 1}@gmail.com" if global_index % 8 != 0 else None,
            "Channel": channel,
            "NeedType": need_type,
            "NeedDescription": description,
            "ProductInterest": interests,
            "PriorityScore": base_score,
            "BasePriorityScore": base_score,
            "PriorityLevel": level,
            "RoutingType": "Auto",
            "AssignedGroup": group_name,
            "AssignedStoreCode": store_code,
            "AssignedUsername": assigned_username,
            "AssignedAt": offset_expr(assigned_offset) if assigned_offset is not None else None,
            "Status": status,
            "SlaDeadline": offset_expr(deadline_offset) if deadline_offset is not None else None,
            "SlaViolated": violation,
            "SlaWarningSentAt": None,
            "CreatedByUsername": created_by_username,
            "CreatedAt": offset_expr(created_offset),
            "UpdatedAt": offset_expr(closed_offset if closed_offset is not None else (assigned_offset + 45 if assigned_offset is not None else created_offset + 60)),
            "ClosedAt": offset_expr(closed_offset) if closed_offset is not None else None,
        }
        rows.append(row)

    if len(rows) != 900:
        raise ValueError(f"Expected 900 leads, got {len(rows)}")
    return rows


LEAD_ROWS = build_leads()


def build_tickets() -> list[dict]:
    cskh_leads = [lead for lead in LEAD_ROWS if lead["AssignedGroup"] == "Cskh"]
    statuses = ["New"] * 35 + ["InProgress"] * 80 + ["WaitingCustomer"] * 40 + ["Escalated"] * 20 + ["Resolved"] * 30 + ["Closed"] * 15
    RNG.shuffle(statuses)
    per_day_seq: dict[str, int] = {}
    rows: list[dict] = []
    users_cs = USERS_BY_ROLE["CS"]

    team_leader_by_region = {
        True: "le.ngoc.mai-tn-cskh-bac",
        False: "pham.thu.hang-tn-cskh-nam",
    }

    for idx, (lead, status) in enumerate(zip(cskh_leads, statuses), start=1):
        assigned_store_code = lead["AssignedStoreCode"]
        north = assigned_store_code in NORTH_STORE_CODES if assigned_store_code else True
        fallback_cs_pool = [u["Username"] for u in users_cs if (u["StoreCode"] in NORTH_STORE_CODES) == north]
        ticket_assigned_username = lead["AssignedUsername"] or fallback_cs_pool[(idx - 1) % len(fallback_cs_pool)]
        assigned_offset = expr_minutes(lead["AssignedAt"])
        if assigned_offset is None:
            assigned_offset = -((idx % 12) * 75 + 60)
        assigned_expr = offset_expr(assigned_offset)

        level = lead["PriorityLevel"]
        deadline_offset = assigned_offset + sla_hours("Cskh", level) * 60
        violation = status in {"Escalated", "Resolved", "Closed"} and (idx % 6 == 0)
        resolved_offset = None
        closed_offset = None
        escalated_to = None
        escalated_at = None
        escalated_reason = None
        satisfaction_score = None
        satisfaction_note = None

        if status == "Escalated":
            escalated_to = team_leader_by_region[north]
            escalated_at = offset_expr(deadline_offset + 30 if violation else assigned_offset + 90)
            escalated_reason = "Khách yêu cầu cấp trên xử lý trực tiếp do ảnh hưởng hoạt động kinh doanh."
        if status in {"Resolved", "Closed"}:
            resolved_offset = deadline_offset + 45 if violation else min(deadline_offset - 30, assigned_offset + 360)
            satisfaction_score = 5 - (idx % 2) if status == "Resolved" else 4 + (idx % 2)
            satisfaction_note = "Khách xác nhận vấn đề đã được xử lý ổn, mong tiếp tục hỗ trợ nhanh các lần sau."
            if status == "Closed":
                closed_offset = resolved_offset + 180

        if status == "InProgress":
            updated_expr = offset_expr(assigned_offset + 60)
        elif status == "WaitingCustomer":
            updated_expr = offset_expr(assigned_offset + 180)
        elif status == "Escalated":
            updated_expr = escalated_at
        elif status == "Resolved":
            updated_expr = offset_expr(resolved_offset)
        elif status == "Closed":
            updated_expr = offset_expr(closed_offset)
        else:
            updated_expr = assigned_expr

        lead_date = NOW_UTC + dt.timedelta(minutes=assigned_offset)
        key = lead_date.strftime("%Y%m%d")
        per_day_seq[key] = per_day_seq.get(key, 900) + 1
        ticket_code = f"TK{key}{per_day_seq[key]:03d}"

        row = {
            "TicketId": guid_for(f"ticket:{ticket_code}"),
            "TicketCode": ticket_code,
            "CustomerName": lead["CustomerName"],
            "CustomerPhone": lead["CustomerPhone"],
            "CustomerAddress": lead["CustomerAddress"],
            "CustomerEmail": lead["CustomerEmail"],
            "Channel": lead["Channel"],
            "NeedType": lead["NeedType"],
            "NeedDescription": lead["NeedDescription"],
            "PriorityScore": lead["PriorityScore"],
            "PriorityLevel": lead["PriorityLevel"],
            "AssignedUsername": ticket_assigned_username,
            "AssignedAt": assigned_expr,
            "AssignedStoreCode": assigned_store_code,
            "Status": status,
            "SlaDeadline": offset_expr(deadline_offset),
            "SlaViolated": violation,
            "SlaWarningSentAt": None,
            "EscalatedToUsername": escalated_to,
            "EscalatedAt": escalated_at,
            "EscalatedReason": escalated_reason,
            "SatisfactionScore": satisfaction_score,
            "SatisfactionNote": satisfaction_note,
            "LeadCode": lead["LeadCode"],
            "CreatedByUsername": lead["CreatedByUsername"],
            "CreatedAt": lead["CreatedAt"],
            "UpdatedAt": updated_expr,
            "ClosedAt": offset_expr(closed_offset) if closed_offset is not None else None,
        }
        rows.append(row)

    if len(rows) != 220:
        raise ValueError(f"Expected 220 tickets, got {len(rows)}")
    return rows


TICKET_ROWS = build_tickets()


def build_follow_up_tasks() -> list[dict]:
    assignable_leads = [lead for lead in LEAD_ROWS if lead["AssignedUsername"]]
    RNG.shuffle(assignable_leads)
    rows: list[dict] = []

    for idx, lead in enumerate(assignable_leads[:300], start=1):
        if idx <= 150:
            task_state = "completed"
        elif idx <= 255:
            task_state = "upcoming"
        else:
            task_state = "overdue"

        if task_state == "completed":
            due_offset = -((idx % 40) * 120 + 180)
            completed_offset = due_offset + 60
            is_completed = True
            completed_at = offset_expr(completed_offset)
        elif task_state == "upcoming":
            due_offset = (idx % 48) * 90 + 60
            is_completed = False
            completed_at = None
        else:
            due_offset = -((idx % 36) * 120 + 120)
            is_completed = False
            completed_at = None

        note_prefix = {
            "Sale": "Liên hệ lại để chốt báo giá và xác nhận nhu cầu thực tế.",
            "Cskh": "Gọi lại khách để cập nhật tiến độ xử lý và xác nhận trải nghiệm.",
            "StoreSupport": "Xác nhận lịch khách đến cửa hàng và người tiếp nhận tại quầy.",
        }[lead["AssignedGroup"]]

        rows.append({
            "TaskId": guid_for(f"task:{lead['LeadCode']}:{idx}"),
            "LeadCode": lead["LeadCode"],
            "AssignedUsername": lead["AssignedUsername"],
            "DueAt": offset_expr(due_offset),
            "Note": f"{note_prefix} Lead {lead['LeadCode']} - {lead['CustomerName']}.",
            "IsCompleted": is_completed,
            "CompletedAt": completed_at,
            "CreatedAt": offset_expr(due_offset - 240 if due_offset > 0 else due_offset - 360),
            "NotificationSentAt": None,
        })

    if len(rows) != 300:
        raise ValueError(f"Expected 300 follow-up tasks, got {len(rows)}")
    return rows


FOLLOW_UP_ROWS = build_follow_up_tasks()


def render_sql() -> str:
    store_seed_rows = [
        {
            "StoreId": guid_for(f"store:{store.code}"),
            "StoreCode": store.code,
            "StoreName": store.name,
            "Address": store.address,
            "Region": store.region,
            "MaxCapacity": 20,
            "IsActive": True,
            "CreatedAt": offset_expr(-5),
        }
        for store in NEW_STORES
    ]

    active_store_rows = [{"StoreCode": store.code} for store in ACTIVE_STORES]
    manager_seed_rows = [{"StoreCode": row["StoreCode"], "Username": row["Username"]} for row in MANAGER_ROWS]

    sections = []
    sections.append(textwrap.dedent(
        f"""\
        -- ============================================================
        -- OmniRoute - Demo Data Seed Script
        -- Sinh bởi: scripts/build_demo_seed.py
        -- Mật khẩu mặc định cho user mới: 123
        -- Batch: {SEED_KEY}
        -- ============================================================
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        BEGIN TRY
            BEGIN TRANSACTION;

            DECLARE @Now DATETIME2 = SYSUTCDATETIME();
            DECLARE @DefaultPasswordHash NVARCHAR(MAX) = N'{PASSWORD_HASH}';

        """
    ))

    sections.append("-- 1. Stores")
    sections.append(emit_table("StoreSeed", [
        ("StoreId", "UNIQUEIDENTIFIER NOT NULL"),
        ("StoreCode", "NVARCHAR(20) NOT NULL"),
        ("StoreName", "NVARCHAR(200) NOT NULL"),
        ("Address", "NVARCHAR(500) NULL"),
        ("Region", "NVARCHAR(100) NULL"),
        ("MaxCapacity", "INT NOT NULL"),
        ("IsActive", "BIT NOT NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
    ], store_seed_rows))
    sections.append(textwrap.dedent(
        """\
        MERGE INTO [Stores] AS [Target]
        USING @StoreSeed AS [Source]
           ON [Target].[StoreCode] = [Source].[StoreCode]
        WHEN MATCHED THEN
            UPDATE SET
                [StoreName] = [Source].[StoreName],
                [Address] = [Source].[Address],
                [Region] = [Source].[Region],
                [MaxCapacity] = [Source].[MaxCapacity],
                [IsActive] = [Source].[IsActive]
        WHEN NOT MATCHED THEN
            INSERT ([Id], [StoreCode], [StoreName], [Address], [Region], [ManagerId], [MaxCapacity], [IsActive], [CreatedAt])
            VALUES ([Source].[StoreId], [Source].[StoreCode], [Source].[StoreName], [Source].[Address], [Source].[Region], NULL, [Source].[MaxCapacity], [Source].[IsActive], [Source].[CreatedAt]);

        """
    ))
    sections.append(emit_table("ActiveStoreSeed", [
        ("StoreCode", "NVARCHAR(20) NOT NULL"),
    ], active_store_rows))

    sections.append("-- 2. Teams")
    sections.append(emit_table("TeamSeed", [
        ("TeamId", "UNIQUEIDENTIFIER NOT NULL"),
        ("TeamName", "NVARCHAR(200) NOT NULL"),
        ("TeamType", "NVARCHAR(20) NOT NULL"),
        ("LeaderUsername", "NVARCHAR(100) NOT NULL"),
        ("StoreCode", "NVARCHAR(20) NULL"),
        ("IsActive", "BIT NOT NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
    ], TEAM_ROWS))
    sections.append(textwrap.dedent(
        """\
        ;WITH TeamLeaderSeed AS (
            SELECT
                s.TeamName,
                s.TeamType,
                s.IsActive,
                s.CreatedAt,
                u.UserId AS LeaderId,
                st.Id AS StoreId
            FROM @TeamSeed s
            INNER JOIN [Users] u ON u.Username = s.LeaderUsername
            LEFT JOIN [Stores] st ON st.StoreCode = s.StoreCode
        )
        UPDATE t
           SET t.[TeamName] = s.[TeamName],
               t.[TeamType] = s.[TeamType],
               t.[StoreId] = s.[StoreId],
               t.[IsActive] = s.[IsActive]
        FROM [Teams] t
        INNER JOIN TeamLeaderSeed s ON s.LeaderId = t.LeaderId;

        ;WITH TeamResolved AS (
            SELECT
                s.TeamId,
                s.TeamName,
                s.TeamType,
                s.LeaderUsername,
                st.Id AS StoreId,
                s.IsActive,
                s.CreatedAt
            FROM @TeamSeed s
            LEFT JOIN [Stores] st ON st.StoreCode = s.StoreCode
        )
        MERGE INTO [Teams] AS [Target]
        USING TeamResolved AS [Source]
           ON [Target].[TeamName] = [Source].[TeamName]
        WHEN MATCHED THEN
            UPDATE SET
                [TeamType] = [Source].[TeamType],
                [StoreId] = [Source].[StoreId],
                [IsActive] = [Source].[IsActive]
        WHEN NOT MATCHED THEN
            INSERT ([Id], [TeamName], [TeamType], [LeaderId], [StoreId], [IsActive], [CreatedAt])
            VALUES ([Source].[TeamId], [Source].[TeamName], [Source].[TeamType], NULL, [Source].[StoreId], [Source].[IsActive], [Source].[CreatedAt]);

        """
    ))

    sections.append("-- 3. Users")
    sections.append(emit_table("UserSeed", [
        ("UserId", "UNIQUEIDENTIFIER NOT NULL"),
        ("Username", "NVARCHAR(100) NOT NULL"),
        ("Email", "NVARCHAR(200) NOT NULL"),
        ("PasswordHash", "NVARCHAR(MAX) NOT NULL"),
        ("FirstName", "NVARCHAR(100) NULL"),
        ("LastName", "NVARCHAR(100) NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
        ("LastLogin", "DATETIME2 NULL"),
        ("RoleName", "NVARCHAR(10) NOT NULL"),
        ("TeamName", "NVARCHAR(200) NULL"),
        ("StoreCode", "NVARCHAR(20) NULL"),
        ("CurrentWorkload", "INT NOT NULL"),
        ("IsActive", "BIT NOT NULL"),
        ("ForcePasswordChange", "BIT NOT NULL"),
        ("LastAssignedAt", "DATETIME2 NULL"),
    ], USER_ROWS))
    sections.append(textwrap.dedent(
        """\
        ;WITH UserResolved AS (
            SELECT
                s.UserId,
                s.Username,
                s.Email,
                s.PasswordHash,
                s.FirstName,
                s.LastName,
                s.CreatedAt,
                s.LastLogin,
                s.CurrentWorkload,
                s.IsActive,
                s.ForcePasswordChange,
                s.LastAssignedAt,
                r.RoleId,
                t.Id AS TeamId,
                st.Id AS StoreId
            FROM @UserSeed s
            INNER JOIN [Roles] r ON r.RoleName = s.RoleName
            LEFT JOIN [Teams] t ON t.TeamName = s.TeamName
            LEFT JOIN [Stores] st ON st.StoreCode = s.StoreCode
        )
        MERGE INTO [Users] AS [Target]
        USING UserResolved AS [Source]
           ON [Target].[Username] = [Source].[Username]
        WHEN MATCHED THEN
            UPDATE SET
                [Email] = [Source].[Email],
                [PasswordHash] = [Source].[PasswordHash],
                [FirstName] = [Source].[FirstName],
                [LastName] = [Source].[LastName],
                [LastLogin] = [Source].[LastLogin],
                [RoleId] = [Source].[RoleId],
                [CurrentWorkload] = [Source].[CurrentWorkload],
                [IsActive] = [Source].[IsActive],
                [StoreId] = [Source].[StoreId],
                [TeamId] = [Source].[TeamId],
                [ForcePasswordChange] = [Source].[ForcePasswordChange],
                [LastAssignedAt] = [Source].[LastAssignedAt]
        WHEN NOT MATCHED THEN
            INSERT ([UserId], [Username], [Email], [PasswordHash], [FirstName], [LastName], [CreatedAt], [LastLogin], [RoleId], [CurrentWorkload], [IsActive], [StoreId], [TeamId], [ForcePasswordChange], [LastAssignedAt])
            VALUES ([Source].[UserId], [Source].[Username], [Source].[Email], [Source].[PasswordHash], [Source].[FirstName], [Source].[LastName], [Source].[CreatedAt], [Source].[LastLogin], [Source].[RoleId], [Source].[CurrentWorkload], [Source].[IsActive], [Source].[StoreId], [Source].[TeamId], [Source].[ForcePasswordChange], [Source].[LastAssignedAt]);

        """
    ))
    sections.append(textwrap.dedent(
        """\
        UPDATE t
           SET t.[LeaderId] = u.[UserId]
        FROM [Teams] t
        INNER JOIN @TeamSeed ts ON ts.TeamName = t.TeamName
        INNER JOIN [Users] u ON u.Username = ts.LeaderUsername;

        """
    ))
    sections.append(emit_table("ManagerSeed", [
        ("StoreCode", "NVARCHAR(20) NOT NULL"),
        ("Username", "NVARCHAR(100) NOT NULL"),
    ], manager_seed_rows))
    sections.append(textwrap.dedent(
        """\
        UPDATE s
           SET s.[ManagerId] = u.[UserId]
        FROM [Stores] s
        INNER JOIN @ManagerSeed ms ON ms.StoreCode = s.StoreCode
        INNER JOIN [Users] u ON u.Username = ms.Username;

        """
    ))

    sections.append("-- 4. User Profiles")
    sections.append(emit_table("UserProfileSeed", [
        ("ProfileId", "UNIQUEIDENTIFIER NOT NULL"),
        ("Username", "NVARCHAR(100) NOT NULL"),
        ("Bio", "NVARCHAR(500) NULL"),
        ("AvatarUrl", "NVARCHAR(1000) NULL"),
        ("DateOfBirth", "DATE NULL"),
        ("Phone", "NVARCHAR(20) NULL"),
        ("Address", "NVARCHAR(500) NULL"),
        ("UpdatedAt", "DATETIME2 NULL"),
    ], PROFILE_ROWS))
    sections.append(textwrap.dedent(
        """\
        ;WITH ProfileResolved AS (
            SELECT
                s.ProfileId,
                u.UserId,
                s.Bio,
                s.AvatarUrl,
                s.DateOfBirth,
                s.Phone,
                s.Address,
                s.UpdatedAt
            FROM @UserProfileSeed s
            INNER JOIN [Users] u ON u.Username = s.Username
        )
        MERGE INTO [UserProfiles] AS [Target]
        USING ProfileResolved AS [Source]
           ON [Target].[UserId] = [Source].[UserId]
        WHEN MATCHED THEN
            UPDATE SET
                [Bio] = [Source].[Bio],
                [AvatarUrl] = [Source].[AvatarUrl],
                [DateOfBirth] = [Source].[DateOfBirth],
                [Phone] = [Source].[Phone],
                [Address] = [Source].[Address],
                [UpdatedAt] = [Source].[UpdatedAt]
        WHEN NOT MATCHED THEN
            INSERT ([ProfileId], [UserId], [Bio], [AvatarUrl], [DateOfBirth], [Phone], [Address], [UpdatedAt])
            VALUES ([Source].[ProfileId], [Source].[UserId], [Source].[Bio], [Source].[AvatarUrl], [Source].[DateOfBirth], [Source].[Phone], [Source].[Address], [Source].[UpdatedAt]);

        """
    ))

    sections.append("-- 5. Master Data")
    sections.append(emit_table("MasterDataSeed", [
        ("Id", "UNIQUEIDENTIFIER NOT NULL"),
        ("Category", "NVARCHAR(30) NOT NULL"),
        ("Code", "NVARCHAR(100) NOT NULL"),
        ("DisplayName", "NVARCHAR(200) NOT NULL"),
        ("Description", "NVARCHAR(500) NULL"),
        ("SortOrder", "INT NOT NULL"),
        ("IsActive", "BIT NOT NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
    ], MASTER_DATA_SEED))
    sections.append(textwrap.dedent(
        """\
        MERGE INTO [MasterDataItems] AS [Target]
        USING @MasterDataSeed AS [Source]
           ON [Target].[Category] = [Source].[Category]
          AND [Target].[Code] = [Source].[Code]
        WHEN MATCHED THEN
            UPDATE SET
                [DisplayName] = [Source].[DisplayName],
                [Description] = [Source].[Description],
                [SortOrder] = [Source].[SortOrder],
                [IsActive] = [Source].[IsActive]
        WHEN NOT MATCHED THEN
            INSERT ([Id], [Category], [Code], [DisplayName], [Description], [SortOrder], [IsActive], [CreatedAt])
            VALUES ([Source].[Id], [Source].[Category], [Source].[Code], [Source].[DisplayName], [Source].[Description], [Source].[SortOrder], [Source].[IsActive], [Source].[CreatedAt]);

        """
    ))

    sections.append("-- 6. Routing Rules")
    sections.append(emit_table("RoutingRuleSeed", [
        ("Id", "UNIQUEIDENTIFIER NOT NULL"),
        ("RuleName", "NVARCHAR(200) NOT NULL"),
        ("Description", "NVARCHAR(1000) NULL"),
        ("PriorityOrder", "INT NOT NULL"),
        ("ConditionChannelJson", "NVARCHAR(MAX) NULL"),
        ("ConditionKeywordsJson", "NVARCHAR(MAX) NULL"),
        ("ActionGroup", "NVARCHAR(20) NOT NULL"),
        ("ActionTeamName", "NVARCHAR(200) NULL"),
        ("IsActive", "BIT NOT NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
        ("UpdatedAt", "DATETIME2 NOT NULL"),
    ], ROUTING_RULE_SEED))
    sections.append(textwrap.dedent(
        """\
        ;WITH RoutingRuleResolved AS (
            SELECT
                s.Id,
                s.RuleName,
                s.Description,
                s.PriorityOrder,
                s.ConditionChannelJson,
                s.ConditionKeywordsJson,
                s.ActionGroup,
                t.Id AS ActionTeamId,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt
            FROM @RoutingRuleSeed s
            LEFT JOIN [Teams] t ON t.TeamName = s.ActionTeamName
        )
        MERGE INTO [RoutingRules] AS [Target]
        USING RoutingRuleResolved AS [Source]
           ON [Target].[PriorityOrder] = [Source].[PriorityOrder]
        WHEN MATCHED THEN
            UPDATE SET
                [RuleName] = [Source].[RuleName],
                [Description] = [Source].[Description],
                [PriorityOrder] = [Source].[PriorityOrder],
                [ConditionChannelJson] = [Source].[ConditionChannelJson],
                [ConditionKeywordsJson] = [Source].[ConditionKeywordsJson],
                [ActionGroup] = [Source].[ActionGroup],
                [ActionTeamId] = [Source].[ActionTeamId],
                [IsActive] = [Source].[IsActive],
                [UpdatedAt] = [Source].[UpdatedAt]
        WHEN NOT MATCHED THEN
            INSERT ([Id], [RuleName], [Description], [PriorityOrder], [ConditionChannelJson], [ConditionKeywordsJson], [ActionGroup], [ActionTeamId], [IsActive], [CreatedAt], [UpdatedAt])
            VALUES ([Source].[Id], [Source].[RuleName], [Source].[Description], [Source].[PriorityOrder], [Source].[ConditionChannelJson], [Source].[ConditionKeywordsJson], [Source].[ActionGroup], [Source].[ActionTeamId], [Source].[IsActive], [Source].[CreatedAt], [Source].[UpdatedAt]);

        """
    ))

    sections.append("-- 7. Leads")
    sections.append(emit_table("LeadSeed", [
        ("LeadId", "UNIQUEIDENTIFIER NOT NULL"),
        ("LeadCode", "NVARCHAR(30) NOT NULL"),
        ("CustomerName", "NVARCHAR(200) NOT NULL"),
        ("CustomerPhone", "NVARCHAR(20) NOT NULL"),
        ("CustomerAddress", "NVARCHAR(500) NULL"),
        ("CustomerEmail", "NVARCHAR(200) NULL"),
        ("Channel", "NVARCHAR(20) NOT NULL"),
        ("NeedType", "NVARCHAR(30) NULL"),
        ("NeedDescription", "NVARCHAR(MAX) NOT NULL"),
        ("ProductInterest", "NVARCHAR(MAX) NULL"),
        ("PriorityScore", "INT NOT NULL"),
        ("BasePriorityScore", "INT NOT NULL"),
        ("PriorityLevel", "NVARCHAR(10) NULL"),
        ("RoutingType", "NVARCHAR(10) NOT NULL"),
        ("AssignedGroup", "NVARCHAR(20) NULL"),
        ("AssignedStoreCode", "NVARCHAR(20) NULL"),
        ("AssignedUsername", "NVARCHAR(100) NULL"),
        ("AssignedAt", "DATETIME2 NULL"),
        ("Status", "NVARCHAR(30) NOT NULL"),
        ("SlaDeadline", "DATETIME2 NULL"),
        ("SlaViolated", "BIT NOT NULL"),
        ("SlaWarningSentAt", "DATETIME2 NULL"),
        ("CreatedByUsername", "NVARCHAR(100) NOT NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
        ("UpdatedAt", "DATETIME2 NOT NULL"),
        ("ClosedAt", "DATETIME2 NULL"),
    ], LEAD_ROWS))
    sections.append(textwrap.dedent(
        """\
        ;WITH LeadResolved AS (
            SELECT
                s.LeadId,
                s.LeadCode,
                s.CustomerName,
                s.CustomerPhone,
                s.CustomerAddress,
                s.CustomerEmail,
                s.Channel,
                s.NeedType,
                s.NeedDescription,
                s.ProductInterest,
                s.PriorityScore,
                s.BasePriorityScore,
                s.PriorityLevel,
                s.RoutingType,
                s.AssignedGroup,
                st.Id AS AssignedStoreId,
                au.UserId AS AssignedUserId,
                s.AssignedAt,
                s.Status,
                s.SlaDeadline,
                s.SlaViolated,
                s.SlaWarningSentAt,
                cu.UserId AS CreatedBy,
                s.CreatedAt,
                s.UpdatedAt,
                s.ClosedAt
            FROM @LeadSeed s
            INNER JOIN [Users] cu ON cu.Username = s.CreatedByUsername
            LEFT JOIN [Users] au ON au.Username = s.AssignedUsername
            LEFT JOIN [Stores] st ON st.StoreCode = s.AssignedStoreCode
        )
        MERGE INTO [Leads] AS [Target]
        USING LeadResolved AS [Source]
           ON [Target].[LeadCode] = [Source].[LeadCode]
        WHEN MATCHED THEN
            UPDATE SET
                [CustomerName] = [Source].[CustomerName],
                [CustomerPhone] = [Source].[CustomerPhone],
                [CustomerAddress] = [Source].[CustomerAddress],
                [CustomerEmail] = [Source].[CustomerEmail],
                [Channel] = [Source].[Channel],
                [NeedType] = [Source].[NeedType],
                [NeedDescription] = [Source].[NeedDescription],
                [ProductInterest] = [Source].[ProductInterest],
                [PriorityScore] = [Source].[PriorityScore],
                [BasePriorityScore] = [Source].[BasePriorityScore],
                [PriorityLevel] = [Source].[PriorityLevel],
                [RoutingType] = [Source].[RoutingType],
                [AssignedGroup] = [Source].[AssignedGroup],
                [AssignedStoreId] = [Source].[AssignedStoreId],
                [AssignedUserId] = [Source].[AssignedUserId],
                [AssignedAt] = [Source].[AssignedAt],
                [Status] = [Source].[Status],
                [SlaDeadline] = [Source].[SlaDeadline],
                [SlaViolated] = [Source].[SlaViolated],
                [SlaWarningSentAt] = [Source].[SlaWarningSentAt],
                [CreatedBy] = [Source].[CreatedBy],
                [CreatedAt] = [Source].[CreatedAt],
                [UpdatedAt] = [Source].[UpdatedAt],
                [ClosedAt] = [Source].[ClosedAt]
        WHEN NOT MATCHED THEN
            INSERT ([Id], [LeadCode], [CustomerName], [CustomerPhone], [CustomerAddress], [CustomerEmail], [Channel], [NeedType], [NeedDescription], [ProductInterest], [PriorityScore], [PriorityLevel], [RoutingType], [AssignedGroup], [AssignedStoreId], [AssignedUserId], [AssignedAt], [Status], [SlaDeadline], [SlaViolated], [SlaWarningSentAt], [CreatedBy], [CreatedAt], [UpdatedAt], [ClosedAt], [BasePriorityScore])
            VALUES ([Source].[LeadId], [Source].[LeadCode], [Source].[CustomerName], [Source].[CustomerPhone], [Source].[CustomerAddress], [Source].[CustomerEmail], [Source].[Channel], [Source].[NeedType], [Source].[NeedDescription], [Source].[ProductInterest], [Source].[PriorityScore], [Source].[PriorityLevel], [Source].[RoutingType], [Source].[AssignedGroup], [Source].[AssignedStoreId], [Source].[AssignedUserId], [Source].[AssignedAt], [Source].[Status], [Source].[SlaDeadline], [Source].[SlaViolated], [Source].[SlaWarningSentAt], [Source].[CreatedBy], [Source].[CreatedAt], [Source].[UpdatedAt], [Source].[ClosedAt], [Source].[BasePriorityScore]);

        """
    ))

    sections.append("-- 8. Tickets")
    sections.append(emit_table("TicketSeed", [
        ("TicketId", "UNIQUEIDENTIFIER NOT NULL"),
        ("TicketCode", "NVARCHAR(30) NOT NULL"),
        ("CustomerName", "NVARCHAR(200) NOT NULL"),
        ("CustomerPhone", "NVARCHAR(20) NOT NULL"),
        ("CustomerAddress", "NVARCHAR(500) NULL"),
        ("CustomerEmail", "NVARCHAR(200) NULL"),
        ("Channel", "NVARCHAR(20) NOT NULL"),
        ("NeedType", "NVARCHAR(30) NULL"),
        ("NeedDescription", "NVARCHAR(MAX) NOT NULL"),
        ("PriorityScore", "INT NOT NULL"),
        ("PriorityLevel", "NVARCHAR(10) NULL"),
        ("AssignedUsername", "NVARCHAR(100) NULL"),
        ("AssignedAt", "DATETIME2 NULL"),
        ("AssignedStoreCode", "NVARCHAR(20) NULL"),
        ("Status", "NVARCHAR(30) NOT NULL"),
        ("SlaDeadline", "DATETIME2 NULL"),
        ("SlaViolated", "BIT NOT NULL"),
        ("SlaWarningSentAt", "DATETIME2 NULL"),
        ("EscalatedToUsername", "NVARCHAR(100) NULL"),
        ("EscalatedAt", "DATETIME2 NULL"),
        ("EscalatedReason", "NVARCHAR(1000) NULL"),
        ("SatisfactionScore", "INT NULL"),
        ("SatisfactionNote", "NVARCHAR(1000) NULL"),
        ("LeadCode", "NVARCHAR(30) NOT NULL"),
        ("CreatedByUsername", "NVARCHAR(100) NOT NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
        ("UpdatedAt", "DATETIME2 NOT NULL"),
        ("ClosedAt", "DATETIME2 NULL"),
    ], TICKET_ROWS))
    sections.append(textwrap.dedent(
        """\
        ;WITH TicketResolved AS (
            SELECT
                s.TicketId,
                s.TicketCode,
                s.CustomerName,
                s.CustomerPhone,
                s.CustomerAddress,
                s.CustomerEmail,
                s.Channel,
                s.NeedType,
                s.NeedDescription,
                s.PriorityScore,
                s.PriorityLevel,
                au.UserId AS AssignedUserId,
                s.AssignedAt,
                st.Id AS AssignedStoreId,
                s.Status,
                s.SlaDeadline,
                s.SlaViolated,
                s.SlaWarningSentAt,
                eu.UserId AS EscalatedTo,
                s.EscalatedAt,
                s.EscalatedReason,
                s.SatisfactionScore,
                s.SatisfactionNote,
                l.Id AS LeadId,
                cu.UserId AS CreatedBy,
                s.CreatedAt,
                s.UpdatedAt,
                s.ClosedAt
            FROM @TicketSeed s
            INNER JOIN [Leads] l ON l.LeadCode = s.LeadCode
            INNER JOIN [Users] cu ON cu.Username = s.CreatedByUsername
            LEFT JOIN [Users] au ON au.Username = s.AssignedUsername
            LEFT JOIN [Users] eu ON eu.Username = s.EscalatedToUsername
            LEFT JOIN [Stores] st ON st.StoreCode = s.AssignedStoreCode
        )
        MERGE INTO [Tickets] AS [Target]
        USING TicketResolved AS [Source]
           ON [Target].[TicketCode] = [Source].[TicketCode]
        WHEN MATCHED THEN
            UPDATE SET
                [CustomerName] = [Source].[CustomerName],
                [CustomerPhone] = [Source].[CustomerPhone],
                [CustomerAddress] = [Source].[CustomerAddress],
                [CustomerEmail] = [Source].[CustomerEmail],
                [Channel] = [Source].[Channel],
                [NeedType] = [Source].[NeedType],
                [NeedDescription] = [Source].[NeedDescription],
                [PriorityScore] = [Source].[PriorityScore],
                [PriorityLevel] = [Source].[PriorityLevel],
                [AssignedUserId] = [Source].[AssignedUserId],
                [AssignedAt] = [Source].[AssignedAt],
                [AssignedStoreId] = [Source].[AssignedStoreId],
                [Status] = [Source].[Status],
                [SlaDeadline] = [Source].[SlaDeadline],
                [SlaViolated] = [Source].[SlaViolated],
                [SlaWarningSentAt] = [Source].[SlaWarningSentAt],
                [EscalatedTo] = [Source].[EscalatedTo],
                [EscalatedAt] = [Source].[EscalatedAt],
                [EscalatedReason] = [Source].[EscalatedReason],
                [SatisfactionScore] = [Source].[SatisfactionScore],
                [SatisfactionNote] = [Source].[SatisfactionNote],
                [LeadId] = [Source].[LeadId],
                [CreatedBy] = [Source].[CreatedBy],
                [CreatedAt] = [Source].[CreatedAt],
                [UpdatedAt] = [Source].[UpdatedAt],
                [ClosedAt] = [Source].[ClosedAt]
        WHEN NOT MATCHED THEN
            INSERT ([Id], [TicketCode], [CustomerName], [CustomerPhone], [CustomerAddress], [CustomerEmail], [Channel], [NeedType], [NeedDescription], [PriorityScore], [PriorityLevel], [AssignedUserId], [AssignedAt], [AssignedStoreId], [Status], [SlaDeadline], [SlaViolated], [SlaWarningSentAt], [EscalatedTo], [EscalatedAt], [EscalatedReason], [SatisfactionScore], [SatisfactionNote], [LeadId], [CreatedBy], [CreatedAt], [UpdatedAt], [ClosedAt])
            VALUES ([Source].[TicketId], [Source].[TicketCode], [Source].[CustomerName], [Source].[CustomerPhone], [Source].[CustomerAddress], [Source].[CustomerEmail], [Source].[Channel], [Source].[NeedType], [Source].[NeedDescription], [Source].[PriorityScore], [Source].[PriorityLevel], [Source].[AssignedUserId], [Source].[AssignedAt], [Source].[AssignedStoreId], [Source].[Status], [Source].[SlaDeadline], [Source].[SlaViolated], [Source].[SlaWarningSentAt], [Source].[EscalatedTo], [Source].[EscalatedAt], [Source].[EscalatedReason], [Source].[SatisfactionScore], [Source].[SatisfactionNote], [Source].[LeadId], [Source].[CreatedBy], [Source].[CreatedAt], [Source].[UpdatedAt], [Source].[ClosedAt]);

        """
    ))

    sections.append("-- 9. Follow-up Tasks")
    sections.append(emit_table("FollowUpSeed", [
        ("TaskId", "UNIQUEIDENTIFIER NOT NULL"),
        ("LeadCode", "NVARCHAR(30) NOT NULL"),
        ("AssignedUsername", "NVARCHAR(100) NOT NULL"),
        ("DueAt", "DATETIME2 NOT NULL"),
        ("Note", "NVARCHAR(1000) NOT NULL"),
        ("IsCompleted", "BIT NOT NULL"),
        ("CompletedAt", "DATETIME2 NULL"),
        ("CreatedAt", "DATETIME2 NOT NULL"),
        ("NotificationSentAt", "DATETIME2 NULL"),
    ], FOLLOW_UP_ROWS))
    sections.append(textwrap.dedent(
        """\
        ;WITH FollowUpResolved AS (
            SELECT
                s.TaskId,
                l.Id AS LeadId,
                u.UserId,
                s.DueAt,
                s.Note,
                s.IsCompleted,
                s.CompletedAt,
                s.CreatedAt,
                s.NotificationSentAt
            FROM @FollowUpSeed s
            INNER JOIN [Leads] l ON l.LeadCode = s.LeadCode
            INNER JOIN [Users] u ON u.Username = s.AssignedUsername
        )
        MERGE INTO [FollowUpTasks] AS [Target]
        USING FollowUpResolved AS [Source]
           ON [Target].[Id] = [Source].[TaskId]
        WHEN MATCHED THEN
            UPDATE SET
                [LeadId] = [Source].[LeadId],
                [UserId] = [Source].[UserId],
                [DueAt] = [Source].[DueAt],
                [Note] = [Source].[Note],
                [IsCompleted] = [Source].[IsCompleted],
                [CompletedAt] = [Source].[CompletedAt],
                [CreatedAt] = [Source].[CreatedAt],
                [NotificationSentAt] = [Source].[NotificationSentAt]
        WHEN NOT MATCHED THEN
            INSERT ([Id], [LeadId], [UserId], [DueAt], [Note], [IsCompleted], [CompletedAt], [CreatedAt], [NotificationSentAt])
            VALUES ([Source].[TaskId], [Source].[LeadId], [Source].[UserId], [Source].[DueAt], [Source].[Note], [Source].[IsCompleted], [Source].[CompletedAt], [Source].[CreatedAt], [Source].[NotificationSentAt]);

        """
    ))

    sections.append("-- 10. Verification")
    sections.append(textwrap.dedent(
        """\
        SELECT N'Users' AS [Entity], (SELECT COUNT(*) FROM @UserSeed) AS [Expected], (SELECT COUNT(*) FROM [Users] u INNER JOIN @UserSeed s ON s.Username = u.Username) AS [Actual]
        UNION ALL SELECT N'UserProfiles', (SELECT COUNT(*) FROM @UserProfileSeed), (SELECT COUNT(*) FROM [UserProfiles] up INNER JOIN [Users] u ON u.UserId = up.UserId INNER JOIN @UserProfileSeed s ON s.Username = u.Username)
        UNION ALL SELECT N'Stores(New)', (SELECT COUNT(*) FROM @StoreSeed), (SELECT COUNT(*) FROM [Stores] st INNER JOIN @StoreSeed s ON s.StoreCode = st.StoreCode)
        UNION ALL SELECT N'Teams', (SELECT COUNT(*) FROM @TeamSeed), (SELECT COUNT(*) FROM [Teams] t INNER JOIN @TeamSeed s ON s.TeamName = t.TeamName)
        UNION ALL SELECT N'MasterData', (SELECT COUNT(*) FROM @MasterDataSeed), (SELECT COUNT(*) FROM [MasterDataItems] m INNER JOIN @MasterDataSeed s ON s.Category = m.Category AND s.Code = m.Code)
        UNION ALL SELECT N'RoutingRules', (SELECT COUNT(*) FROM @RoutingRuleSeed), (SELECT COUNT(*) FROM [RoutingRules] r INNER JOIN @RoutingRuleSeed s ON s.RuleName = r.RuleName)
        UNION ALL SELECT N'Leads', (SELECT COUNT(*) FROM @LeadSeed), (SELECT COUNT(*) FROM [Leads] l INNER JOIN @LeadSeed s ON s.LeadCode = l.LeadCode)
        UNION ALL SELECT N'Tickets', (SELECT COUNT(*) FROM @TicketSeed), (SELECT COUNT(*) FROM [Tickets] t INNER JOIN @TicketSeed s ON s.TicketCode = t.TicketCode)
        UNION ALL SELECT N'FollowUpTasks', (SELECT COUNT(*) FROM @FollowUpSeed), (SELECT COUNT(*) FROM [FollowUpTasks] f INNER JOIN @FollowUpSeed s ON s.TaskId = f.Id);

        COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;
            THROW;
        END CATCH;
        """
    ))

    return "\n".join(sections).strip() + "\n"


def main() -> None:
    sql = render_sql()
    OUTPUT_PATH.write_text(sql, encoding="utf-8-sig")
    print(f"Wrote {OUTPUT_PATH}")


if __name__ == "__main__":
    main()
