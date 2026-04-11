const sections = [...document.querySelectorAll("[data-section]")];
const links = [...document.querySelectorAll("[data-nav-link]")];
const railCurrent = document.querySelector("[data-rail-current]");
const railMode = document.querySelector("[data-rail-mode]");

function setActiveSection(section) {
  if (!section) {
    return;
  }

  const id = section.id;
  links.forEach((link) => {
    link.classList.toggle("is-active", link.dataset.target === id);
  });

  if (railCurrent) {
    railCurrent.textContent = section.dataset.title || section.id;
  }

  if (railMode) {
    railMode.textContent = section.dataset.mode || "";
  }
}

if (sections.length > 0) {
  setActiveSection(sections[0]);

  const observer = new IntersectionObserver((entries) => {
    const visible = entries
      .filter((entry) => entry.isIntersecting)
      .sort((left, right) => right.intersectionRatio - left.intersectionRatio);

    if (visible.length > 0) {
      setActiveSection(visible[0].target);
    }
  }, {
    rootMargin: "-30% 0px -50% 0px",
    threshold: [0.15, 0.35, 0.6],
  });

  sections.forEach((section) => observer.observe(section));
}