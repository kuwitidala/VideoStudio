const contactForm = $("#contact-form");
const contactMsg = $(".form-msg", contactForm);
const currentUser = getUser();

if (currentUser) {
    contactForm.elements.name.value = currentUser.fullName || "";
    contactForm.elements.email.value = currentUser.email || "";
}

contactForm.addEventListener("submit", async function (event) {
    event.preventDefault();
    setMsg(contactMsg, "");

    const body = {
        name: contactForm.elements.name.value.trim(),
        email: contactForm.elements.email.value.trim(),
        message: contactForm.elements.message.value.trim()
    };

    try {
        await api("/api/contact", { method: "POST", body: body });
        contactForm.elements.message.value = "";
        setMsg(contactMsg, "Сообщение отправлено. Мы ответим на указанную почту.", "ok");
    } catch (error) {
        setMsg(contactMsg, error.message, "error");
    }
});
