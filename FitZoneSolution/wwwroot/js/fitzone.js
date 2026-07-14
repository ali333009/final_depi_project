// FitZone frontend behaviors

// Navbar blur-on-scroll
(function () {
    var nav = document.getElementById('mainNav');
    if (!nav) return;
    var onScroll = function () {
        if (window.scrollY > 12) nav.classList.add('scrolled');
        else nav.classList.remove('scrolled');
    };
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
})();

// Animated counters (use class="fz-counter" data-target="1247" data-suffix="+")
(function () {
    var counters = document.querySelectorAll('.fz-counter');
    if (!counters.length) return;
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            var el = entry.target;
            var target = parseInt(el.dataset.target || '0', 10);
            var suffix = el.dataset.suffix || '';
            var prefix = el.dataset.prefix || '';
            var duration = 2000;
            var start = performance.now();
            function tick(now) {
                var t = Math.min((now - start) / duration, 1);
                var v = Math.floor(target * (1 - Math.pow(1 - t, 3)));
                el.textContent = prefix + v.toLocaleString() + suffix;
                if (t < 1) requestAnimationFrame(tick);
            }
            requestAnimationFrame(tick);
            observer.unobserve(el);
        });
    }, { threshold: 0.5 });
    counters.forEach(function (c) { observer.observe(c); });
})();

// Reveal on scroll
(function () {
    var items = document.querySelectorAll('[data-reveal]');
    if (!items.length) return;
    var obs = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
            if (e.isIntersecting) {
                e.target.style.opacity = '1';
                e.target.style.transform = 'translateY(0)';
                obs.unobserve(e.target);
            }
        });
    }, { threshold: 0.15 });
    items.forEach(function (it) {
        it.style.opacity = '0';
        it.style.transform = 'translateY(24px)';
        it.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        obs.observe(it);
    });
})();
