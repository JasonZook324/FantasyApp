(function () {
  // Persist theme in localStorage, toggle 'dark' class on <html>
  const key = 'theme';
  const root = document.documentElement;

  function apply(theme) {
    if (theme === 'dark') root.classList.add('dark');
    else root.classList.remove('dark');
  }

  const saved = localStorage.getItem(key) || 'dark';
  apply(saved);

  window.addEventListener('DOMContentLoaded', () => {
    const btn = document.getElementById('theme-toggle');
    if (!btn) return;
    btn.addEventListener('click', () => {
      const next = root.classList.contains('dark') ? 'light' : 'dark';
      localStorage.setItem(key, next);
      apply(next);
    });
  });
})();