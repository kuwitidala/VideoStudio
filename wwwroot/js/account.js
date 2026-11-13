const page = $("#account");

function nextPage() {
    const next = new URLSearchParams(location.search).get("next");
    return /^[a-z-]+$/.test(next || "") ? "/" + next : null;
}

//login and register
function showAuth(mode) {
    const isLogin = mode === "login";
    page.innerHTML =
        '<div class="auth-box">' +
        '<div class="tabs">' +
        '<button class="tab' + (isLogin ? " on" : "") + '" data-mode="login" type="button">Вход</button>' +
        '<button class="tab' + (isLogin ? "" : " on") + '" data-mode="register" type="button">Регистрация</button></div>' +
        '<form class="form" novalidate>' +
        (isLogin ? "" : '<div class="field"><label for="a-name">Имя</label><input id="a-name" name="fullName" type="text" autocomplete="name"></div>') +
        '<div class="field"><label for="a-email">Email</label><input id="a-email" name="email" type="email" autocomplete="email"></div>' +
        '<div class="field"><label for="a-pass">Пароль</label><input id="a-pass" name="password" type="password" autocomplete="' + (isLogin ? "current-password" : "new-password") + '"></div>' +
        (isLogin ? "" : '<p class="muted">Не менее 8 символов: заглавная и строчная буква, цифра и спецсимвол (например, !).</p>') +
        '<button class="btn btn-primary" type="submit">' + (isLogin ? "Войти" : "Создать аккаунт") + "</button>" +
        '<p class="form-msg" role="status"></p></form></div>';

    $$(".tab", page).forEach(function (tab) {
        tab.addEventListener("click", function () {
            showAuth(tab.dataset.mode);
        });
    });

    const form = $("form", page);
    const msg = $(".form-msg", page);

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        setMsg(msg, "");
        const email = form.elements.email.value.trim();
        const password = form.elements.password.value;

        if (!email || !password || (!isLogin && !form.elements.fullName.value.trim())) {
            setMsg(msg, "Заполните все поля.", "error");
            return;
        }

        try {
            if (!isLogin) {
                await api("/api/account/register", {
                    method: "POST",
                    body: { fullName: form.elements.fullName.value.trim(), email: email, password: password }
                });
            }
            await login(email, password);
        } catch (error) {
            setMsg(msg, error.message, "error");
        }
    });
}

async function login(email, password) {
    clearSession();
    const data = await api("/api/account/login", { method: "POST", body: { email: email, password: password } });
    localStorage.setItem("vs_token", data.token);
    localStorage.setItem("vs_user", JSON.stringify({ email: data.email, fullName: data.fullName }));

    // Роли берём из профиля
    const profile = await api("/api/profile");
    saveUser({ id: profile.id, roles: profile.roles });

    const next = nextPage();
    if (next) {
        location.href = next;
    } else {
        location.reload();
    }
}

//profile

function showCabinet() {
    const user = getUser();
    const roleName = isAdmin() ? "Администратор" : isStaff() ? "Менеджер" : "Клиент";

    page.innerHTML =
        '<div class="user-bar"><div><h2>' + esc(user.fullName || "Личный кабинет") + "</h2>" +
        '<span class="muted">' + esc(user.email) + ", " + roleName + "</span></div>" +
        '<div class="user-bar-btns">' +
        '<button class="btn btn-line" id="logout" type="button">Выйти</button></div></div>' +
        '<div class="tabs">' +
        '<button class="tab on" data-tab="bookings" type="button">Мои записи</button>' +
        '<button class="tab" data-tab="notes" type="button">Уведомления</button>' +
        '<button class="tab" data-tab="profile" type="button">Профиль</button></div>' +
        '<div id="tab-body"></div>';

    $("#logout").addEventListener("click", logout);

    const tabs = {
        bookings: drawBookings,
        notes: drawNotes,
        profile: drawProfile
    };

    $$(".tab", page).forEach(function (tab) {
        tab.addEventListener("click", function () {
            $$(".tab", page).forEach(function (t) {
                t.classList.toggle("on", t === tab);
            });
            tabs[tab.dataset.tab]($("#tab-body"));
        });
    });

    drawBookings($("#tab-body"));
}




//my notes
async function drawBookings(body) {
    body.innerHTML = '<p class="muted">Загрузка…</p>';
    try {
        const list = await api("/api/bookings/my");
        if (!list.length) {
            body.innerHTML = '<div class="empty">Записей пока нет. <a href="/booking">Записаться на услугу</a></div>';
            return;
        }
        body.innerHTML = list.map(function (b) {
            const when = b.schedule ? b.schedule.startAt : b.bookingDate;
            const canCancel = b.status === "Pending" || b.status === "Confirmed";
            return '<div class="item"><div>' +
                "<h3>" + esc(b.service ? b.service.name : "Услуга") + "</h3>" +
                "<p>" + fmtDateTime(when) + "</p>" +
                (b.comment ? "<p>" + esc(b.comment) + "</p>" : "") + "</div>" +
                '<div class="inline-row"><span class="badge badge-' + b.status + '">' + (STATUS_NAMES[b.status] || b.status) + "</span>" +
                (canCancel ? '<button class="btn btn-danger btn-sm" data-cancel="' + b.id + '" type="button">Отменить</button>' : "") +
                "</div></div>";
        }).join("");

        body.onclick = async function (event) {
            const button = event.target.closest("[data-cancel]");
            if (!button || !confirm("Отменить запись?")) {
                return;
            }
            try {
                await api("/api/bookings/" + button.dataset.cancel + "/cancel", { method: "PUT" });
                toast("Запись отменена", "ok");
                drawBookings(body);
            } catch (error) {
                toast(error.message, "error");
            }
        };
    } catch (error) {
        showError(body, error);
    }
}

//notification
async function drawNotes(body) {
    body.innerHTML = '<p class="muted">Загрузка…</p>';

    try {
        const list = await api("/api/notifications");

        if (!list.length) {
            body.innerHTML = '<div class="empty">Уведомлений нет.</div>';
            return;
        }

        body.innerHTML = list.map(function (n) {
            return '<div class="item">' +
                '<div>' +
                '<h3>' + esc(n.title) + '</h3>' +
                '<p>' + esc(n.message) + '</p>' +
                '<p><small>' + fmtDateTime(n.createdAt) + '</small></p>' +
                '</div>' +
                '</div>';
        }).join("");

    } catch (error) {
        showError(body, error);
    }
}

//profile

function drawProfile(body) {
    const user = getUser();
    body.onclick = null;
    body.innerHTML =
        '<div class="cards cards-2">' +
        '<form class="card form" id="profile-form" novalidate><h3>Личные данные</h3>' +
        '<div class="field"><label for="p-name">Имя</label><input id="p-name" name="fullName" type="text" value="' + esc(user.fullName) + '"></div>' +
        '<div class="field"><label for="p-phone">Телефон</label><input id="p-phone" name="phone" type="tel" placeholder="+7 900 000-00-00"></div>' +
        '<button class="btn btn-primary" type="submit">Сохранить</button><p class="form-msg" role="status"></p></form>' +
        '<form class="card form" id="password-form" novalidate><h3>Смена пароля</h3>' +
        '<div class="field"><label for="p-old">Текущий пароль</label><input id="p-old" name="current" type="password" autocomplete="current-password"></div>' +
        '<div class="field"><label for="p-new">Новый пароль</label><input id="p-new" name="next" type="password" autocomplete="new-password"></div>' +
        '<button class="btn btn-primary" type="submit">Сменить пароль</button><p class="form-msg" role="status"></p></form></div>';

    const profileForm = $("#profile-form");
    profileForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        const msg = $(".form-msg", profileForm);
        const fullName = profileForm.elements.fullName.value.trim();
        const phone = profileForm.elements.phone.value.trim();
        try {
            await api("/api/profilemanagement", {
                method: "PUT",
                body: { fullName: fullName, phoneNumber: phone || null }
            });
            saveUser({ fullName: fullName });
            setMsg(msg, "Профиль обновлён.", "ok");
        } catch (error) {
            setMsg(msg, error.message, "error");
        }
    });

    const passwordForm = $("#password-form");
    passwordForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        const msg = $(".form-msg", passwordForm);
        try {
            await api("/api/profilemanagement/password", {
                method: "PUT",
                body: { currentPassword: passwordForm.elements.current.value, newPassword: passwordForm.elements.next.value }
            });
            passwordForm.reset();
            setMsg(msg, "Пароль изменён.", "ok");
        } catch (error) {
            setMsg(msg, error.message, "error");
        }
    });
}

if (getUser()) {
    showCabinet();
} else {
    showAuth("login");
}
