const SLIDES = [
    {
        label: "Услуги",
        video: "video/video1.mp4",
        scene: "scene-1",
        caption: "Свадебный съёмка",
        title: "Видеосъёмка для свадеб, рекламы и мероприятий",
        text: "Подберём формат, снимем и смонтируем. Вы получите готовый ролик в оговорённый срок.",
        button: "Выбрать услугу",
        href: "/services"
    },
    {
        label: "Фуд-съёмка",
        video: "video/video2.mp4",
        scene: "scene-2",
        caption: "Вкус в каждом кадре",
        title: "Создаём аппетитные видеоролики о еде",
        text: "Эстетичная съёмка блюд, акцент на деталях и профессиональный монтаж, которые подчеркнут вкус и привлекательность ваших продуктов.",
        button: "Наши услуги",
        href: "/services"
    },
    {
        label: "Работы",
        video: "video/video3.mp4",
        scene: "scene-3",
        caption: "Рекламный ролик",
        title: "Посмотрите, как выглядят наши проекты",
        text: "Рекламные ролики, музыкальные клипы, фуд-съёмка и видеосъёмка мероприятий. Познакомьтесь с нашими работами.",
        button: "Открыть портфолио",
        href: "/portfolio"
    }
];

const heroText = $("#hero-text");
const heroFrame = $("#hero-frame");
const heroCaption = $("#hero-caption");
const heroNav = $("#hero-nav");
let current = 0;
let autoplay = null;

heroNav.innerHTML = SLIDES.map(function (slide, index) {
    return '<button class="hero-dot" type="button" data-index="' + index + '"><span>' + slide.label + "</span></button>";
}).join("");

function showSlide(index) {
    current = index;
    const slide = SLIDES[index];

    heroText.classList.add("fade");
    setTimeout(function () {
        heroText.innerHTML =
            "<h1>" + slide.title + "</h1><p>" + slide.text + "</p>" +
            '<div class="hero-actions">' +
            '<a class="btn btn-light" href="' + slide.href + '">' + slide.button + "</a>" +
            '<a class="btn btn-line" href="/booking">Записаться</a></div>';
        heroText.classList.remove("fade");
    }, 250);

    heroFrame.className = "viewfinder " + slide.scene;
    heroCaption.textContent = slide.caption;
    const video = $("#hero-frame .vf-video");
    video.src = slide.video;
    video.play().catch(function () { });
    $$(".hero-dot", heroNav).forEach(function (dot, i) {
        dot.classList.toggle("active", i === index);
    });
}

function restartAutoplay() {
    clearInterval(autoplay);
    if (!window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        autoplay = setInterval(function () {
            showSlide((current + 1) % SLIDES.length);
        }, 7000);
    }
}

heroNav.addEventListener("click", function (event) {
    const dot = event.target.closest(".hero-dot");
    if (dot) {
        showSlide(Number(dot.dataset.index));
        restartAutoplay();
    }
});

const first = SLIDES[0];
heroText.innerHTML =
    "<h1>" + first.title + "</h1><p>" + first.text + "</p>" +
    '<div class="hero-actions"><a class="btn btn-light" href="' + first.href + '">' + first.button + "</a>" +
    '<a class="btn btn-line" href="/booking">Записаться</a></div>';
heroCaption.textContent = first.caption;
$$(".hero-dot", heroNav)[0].classList.add("active");
restartAutoplay();


let seconds = 0;
setInterval(function () {
    seconds++;
    const h = String(Math.floor(seconds / 3600)).padStart(2, "0");
    const m = String(Math.floor(seconds / 60) % 60).padStart(2, "0");
    const s = String(seconds % 60).padStart(2, "0");
    $("#hero-time").textContent = h + ":" + m + ":" + s;
}, 1000);
