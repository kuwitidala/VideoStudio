if (!isStaff()) {
    location.href = "account.html?next=admin.html";
}


const tabsBox = $("#admin-tabs");
const panel = $("#admin-panel");

const tabFunctions = {
    bookings: drawBookings,
    schedule: drawSchedule,
    services: drawServices,
    portfolio: drawPortfolio,
    reviews: drawReviews,
    messages: drawMessages,
    users: drawUsers
};

function openTab(id) {
    if (id === "services" ||  id === "portfolio" ||  id === "users") {
        if (!isAdmin()) {
            id = "bookings";
        }
    }
    const draw = tabFunctions[id];
    if (!draw) {
        id = "bookings";
    }
    const tab = tabFunctions[id];
    $$(".tab", tabsBox).forEach(function (button) {
        button.classList.toggle("on", button.dataset.id === id);
    });

    history.replaceState(null, "", "#" + id);

    panel.innerHTML = "";

    const view = document.createElement("div");
    panel.appendChild(view);
    view.innerHTML = '<p class="muted">Загрузка…</p>';

    tab(view).catch(function (error) {
        showError(view, error);
    });
}
tabsBox.addEventListener("click", function (event) {
    const button = event.target.closest(".tab");

    if (button) {
        openTab(button.dataset.id);
    }
});
tabsBox.addEventListener("click", function (event) {
    const button = event.target.closest(".tab");
    if (button) {
        openTab(button.dataset.id);
    }
});

function table(headers, rows) {
    if (!rows.length) {
        return '<div class="empty">Здесь пока пусто.</div>';
    }
    return '<div class="table-wrap"><table><thead><tr>' +
        headers.map(function (h) { return "<th>" + h + "</th>"; }).join("") +
        "</tr></thead><tbody>" +
        rows.map(function (row) {
            return "<tr>" + row.map(function (cell) { return "<td>" + cell + "</td>"; }).join("") + "</tr>";
        }).join("") +
        "</tbody></table></div>";
}

function actionButton(label, action, id, danger) {
    return '<button class="btn btn-sm ' + (danger ? "btn-danger" : "btn-line") + '" type="button" data-act="' +
        action + '" data-id="' + esc(id) + '">' + label + "</button>";
}

async function run(view, draw, request, okText) {
    try {
        await request();
        toast(okText, "ok");
        draw(view);
    } catch (error) {
        toast(error.message, "error");
    }
}
async function userNames() {
    const names = {};
    if (isAdmin()) {
        try {
            const users = await api("/api/users");
            users.forEach(function (u) {
                names[u.id] = (u.fullName || "Без имени") + " (" + u.email + ")";
            });
        } catch (error) {
        }
    }
    return names;
}

//applications
async function drawBookings(view) {
    const results = await Promise.all([api("/api/bookings"), userNames()]);
    const list = results[0];
    const names = results[1];

    view.innerHTML = "<h2>Заявки на услуги</h2>" + table(
        ["Клиент", "Услуга", "Дата и время", "Комментарий", "Статус", "Действия"],
        list.map(function (b) {
            let buttons = "—";
            if (b.status === "Pending") {
                buttons = actionButton("Подтвердить", "Confirmed", b.id) + actionButton("Отклонить", "Rejected", b.id, true);
            } else if (b.status === "Confirmed") {
                buttons = actionButton("Отменить", "Cancelled", b.id, true);
            }
            return [
                esc(names[b.userId] || "Клиент " + b.userId.slice(0, 6)),
                esc(b.service ? b.service.name : "—"),
                fmtDateTime(b.schedule ? b.schedule.startAt : b.bookingDate),
                esc(b.comment || "—"),
                '<span class="badge badge-' + b.status + '">' + (STATUS_NAMES[b.status] || b.status) + "</span>",
                '<div class="actions">' + buttons + "</div>"
            ];
        })
    );

    view.onclick = function (event) {
        const button = event.target.closest("[data-act]");
        if (button) {
            run(view, drawBookings, function () {
                return api("/api/bookings/" + button.dataset.id + "/status", { method: "PUT", body: button.dataset.act });
            }, "Статус обновлён");
        }
    };
}

//schedule
async function drawSchedule(view) {
    const slots = await api("/api/schedule");

    view.innerHTML =
        "<h2>Расписание</h2>" +
        '<div class="admin-form">' +
        '<form class="form" novalidate>' +
        '<div class="field">' +
        '<label for="s-date">Дата</label>' +
        '<input id="s-date" name="date" type="date" required>' +
        '</div>' +
        '<div class="field">' +
        '<label for="s-time">Время начала</label>' +
        '<input id="s-time" name="time" type="time" value="10:00" required>' +
        '</div>' +
        '<div class="wide">' +
        '<button class="btn btn-primary" type="submit">Добавить время</button>' +
        '</div>' +
        '<p class="form-msg wide" role="status"></p>' +
        '</form></div>' +
        "<h3>Свободные слоты</h3>" +
        table(["Дата", "Время", ""], slots.map(function (slot) {
            return [
                fmtDay(slot.startAt),
                fmtTime(slot.startAt) + "–" + fmtTime(slot.endAt),
                actionButton("Удалить", "delete", slot.id, true)
            ];
        }));

    const form = $("form", view);
    const msg = $(".form-msg", view);

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        const date = form.elements.date.value;
        const time = form.elements.time.value;

        if (!date || !time) {
            setMsg(msg, "Выберите дату и время.", "error");
            return;
        }

        const start = new Date(date + "T" + time);
        const end = new Date(start.getTime() + 2 * 60 * 60 * 1000);

        try {
            await api("/api/schedule", {
                method: "POST",
                body: {
                    startAt: start.toISOString(),
                    endAt: end.toISOString()
                }
            });

            toast("Время добавлено", "ok");
            await drawSchedule(view);
        } catch (error) {
            setMsg(msg, error.message || "Не удалось добавить время.", "error");
        }
    });

    view.onclick = function (event) {
        const button = event.target.closest('[data-act="delete"]');

        if (button && confirm("Удалить слот?")) {
            run(view, drawSchedule, function () {
                return api("/api/schedule/" + button.dataset.id, {
                    method: "DELETE"
                });
            }, "Слот удалён");
        }
    };
}

//services and portfolio
async function drawCrud(view, config) {
    const items = await api(config.url);
    let editId = null;

    view.innerHTML =
        "<h2>" + config.title + "</h2>" +
        '<div class="admin-form"><h3 data-form-title></h3><form class="form" novalidate>' +
        config.fields.map(function (f) {
            const input = f.type === "textarea"
                ? '<textarea id="f-' + f.name + '" name="' + f.name + '"></textarea>'
                : '<input id="f-' + f.name + '" name="' + f.name + '" type="' + f.type + '"' + (f.type === "number" ? ' min="0" step="1"' : "") + ">";
            return '<div class="field' + (f.wide ? " wide" : "") + '"><label for="f-' + f.name + '">' + f.label + "</label>" + input + "</div>";
        }).join("") +
        '<div class="wide inline-row"><button class="btn btn-primary" type="submit">Сохранить</button>' +
        '<button class="btn btn-line hidden" type="button" data-cancel>Отменить редактирование</button></div>' +
        '<p class="form-msg wide" role="status"></p></form></div>' +
        table(config.columns.map(function (c) { return c[0]; }).concat([""]), items.map(function (item) {
            return config.columns.map(function (c) { return c[1](item); }).concat([
                '<div class="actions">' + actionButton("Изменить", "edit", item.id) + actionButton("Удалить", "delete", item.id, true) + "</div>"
            ]);
        }));

    const form = $("form", view);
    const msg = $(".form-msg", view);
    const formTitle = $("[data-form-title]", view);
    const cancel = $("[data-cancel]", view);
    formTitle.textContent = config.addTitle;

    function resetForm() {
        editId = null;
        form.reset();
        formTitle.textContent = config.addTitle;
        cancel.classList.add("hidden");
    }

    cancel.addEventListener("click", resetForm);

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        const body = {};
        config.fields.forEach(function (f) {
            const value = form.elements[f.name].value.trim();
            body[f.name] = f.type === "number" ? Number(value) : (value || (f.required ? "" : null));
        });
        const missing = config.fields.some(function (f) {
            return f.required && !body[f.name] && body[f.name] !== 0;
        });
        if (missing) {
            setMsg(msg, "Заполните обязательные поля.", "error");
            return;
        }
        run(view, function (v) { drawCrud(v, config); }, function () {
            return editId
                ? api(config.url + "/" + editId, { method: "PUT", body: body })
                : api(config.url, { method: "POST", body: body });
        }, "Сохранено");
    });

    view.onclick = function (event) {
        const button = event.target.closest("[data-act]");
        if (!button) {
            return;
        }
        const id = Number(button.dataset.id);
        if (button.dataset.act === "edit") {
            const item = items.find(function (i) { return i.id === id; });
            config.fields.forEach(function (f) {
                form.elements[f.name].value = item[f.name] === null || item[f.name] === undefined ? "" : item[f.name];
            });
            editId = id;
            formTitle.textContent = "Редактирование";
            cancel.classList.remove("hidden");
            form.scrollIntoView({ behavior: "smooth", block: "center" });
        }
        if (button.dataset.act === "delete" && confirm("Удалить запись?")) {
            run(view, function (v) { drawCrud(v, config); }, function () {
                return api(config.url + "/" + id, { method: "DELETE" });
            }, "Удалено");
        }
    };
}

function drawServices(view) {
    return drawCrud(view, {
        url: "/api/services",
        title: "Услуги",
        addTitle: "Новая услуга",
        fields: [
            { name: "name", label: "Название", type: "text", required: true },
            { name: "price", label: "Цена, ₽", type: "number", required: true },
            { name: "description", label: "Описание", type: "textarea", wide: true, required: true },
            { name: "imageUrl", label: "Ссылка на изображение", type: "text", wide: true }
        ],
        columns: [
            ["Название", function (s) { return esc(s.name); }],
            ["Описание", function (s) { return esc(s.description); }],
            ["Цена", function (s) { return money(s.price); }]
        ]
    });
}

function drawPortfolio(view) {
    return drawCrud(view, {
        url: "/api/portfolio",
        title: "Портфолио",
        addTitle: "Новая работа",
        fields: [
            { name: "title", label: "Название", type: "text", required: true, wide: true },
            { name: "description", label: "Описание", type: "textarea", wide: true, required: true },
            { name: "imageUrl", label: "Ссылка на обложку", type: "text", required: true },
            { name: "videoUrl", label: "Ссылка на видео", type: "text" }
        ],
        columns: [
            ["Название", function (w) { return esc(w.title); }],
            ["Описание", function (w) { return esc(w.description); }],
            ["Видео", function (w) { return w.videoUrl ? "есть" : "—"; }]
        ]
    });
}

//reviews

async function drawReviews(view) {
    const results = await Promise.all([api("/api/reviews/all"), userNames()]);
    const names = results[1];

    view.innerHTML = "<h2>Модерация отзывов</h2>" + table(
        ["Автор", "Оценка", "Отзыв", "Дата", "Статус", "Действия"],
        results[0].map(function (r) {
            return [
                esc(names[r.userId] || "Клиент " + r.userId.slice(0, 6)),
                stars(r.rating),
                esc(r.text),
                fmtDateTime(r.createdAt),
                r.isApproved ? '<span class="badge badge-Confirmed">Опубликован</span>' : '<span class="badge badge-Pending">На модерации</span>',
                '<div class="actions">' + (r.isApproved ? "" : actionButton("Опубликовать", "approve", r.id)) + actionButton("Удалить", "delete", r.id, true) + "</div>"
            ];
        })
    );

    view.onclick = function (event) {
        const button = event.target.closest("[data-act]");
        if (!button) {
            return;
        }
        if (button.dataset.act === "approve") {
            run(view, drawReviews, function () {
                return api("/api/reviews/" + button.dataset.id + "/approve", { method: "PUT" });
            }, "Отзыв опубликован");
        }
        if (button.dataset.act === "delete" && confirm("Удалить отзыв?")) {
            run(view, drawReviews, function () {
                return api("/api/reviews/" + button.dataset.id, { method: "DELETE" });
            }, "Отзыв удалён");
        }
    };
}

//messages

async function drawMessages(view) {
    const list = await api("/api/contact");

    view.innerHTML = "<h2>Сообщения</h2>" + table(
        ["От кого", "Сообщение", "Дата", "Статус", "Действия"],
        list.map(function (m) {
            return [
                esc(m.name) + '<br><a href="mailto:' + esc(m.email) + '">' + esc(m.email) + "</a>",
                esc(m.message),
                fmtDateTime(m.createdAt),
                m.isRead ? "Прочитано" : '<span class="badge badge-Pending">Новое</span>',
                '<div class="actions">' + (m.isRead ? "" : actionButton("Прочитано", "read", m.id)) +
                    (isAdmin() ? actionButton("Удалить", "delete", m.id, true) : "") + "</div>"
            ];
        })
    );

    view.onclick = function (event) {
        const button = event.target.closest("[data-act]");
        if (!button) {
            return;
        }
        if (button.dataset.act === "read") {
            run(view, drawMessages, function () {
                return api("/api/contact/" + button.dataset.id + "/read", { method: "PUT" });
            }, "Отмечено как прочитанное");
        }
        if (button.dataset.act === "delete" && confirm("Удалить сообщение?")) {
            run(view, drawMessages, function () {
                return api("/api/contact/" + button.dataset.id, { method: "DELETE" });
            }, "Сообщение удалено");
        }
    };
}

//users

async function drawUsers(view) {
    const users = await api("/api/users");
    const me = getUser().id;

    view.innerHTML = "<h2>Пользователи</h2>" + table(
        ["Имя", "Email", "Роль", "Действия"],
        users.map(function (u) {
            const mine = u.id === me;
            const current = (u.roles || [])[0] || "";
            return [
                esc(u.fullName || "—"),
                esc(u.email),
                '<select data-role-for="' + esc(u.id) + '" aria-label="Роль"' + (mine ? " disabled" : "") + ">" +
                    (current ? "" : '<option value="">Выберите…</option>') +
                    ["User", "Manager", "Admin"].map(function (role) {
                        return '<option value="' + role + '"' + (role === current ? " selected" : "") + ">" +
                            { User: "Пользователь", Manager: "Менеджер", Admin: "Администратор" }[role] + "</option>";
                    }).join("") + "</select>",
                mine ? "Это вы" : '<div class="actions">' + actionButton("Сохранить роль", "role", u.id) + actionButton("Удалить", "delete", u.id, true) + "</div>"
            ];
        })
    );

    view.onclick = function (event) {
        const button = event.target.closest("[data-act]");
        if (!button) {
            return;
        }
        if (button.dataset.act === "role") {
            const select = $('[data-role-for="' + button.dataset.id + '"]', view);
            if (!select.value) {
                toast("Выберите роль", "error");
                return;
            }
            run(view, drawUsers, function () {
                return api("/api/users/" + button.dataset.id + "/role", { method: "PUT", body: { role: select.value } });
            }, "Роль изменена");
        }
        if (button.dataset.act === "delete" && confirm("Удалить пользователя?")) {
            run(view, drawUsers, function () {
                return api("/api/users/" + button.dataset.id, { method: "DELETE" });
            }, "Пользователь удалён");
        }
    };
}

if (isStaff()) {
    openTab(location.hash.slice(1));
}