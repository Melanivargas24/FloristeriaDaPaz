// Mobile responsiveness enhancements
document.addEventListener('DOMContentLoaded', function() {
    
    // Mejorar navegación móvil
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    
    if (navbarToggler && navbarCollapse) {
        // Cerrar menú al hacer clic fuera
        document.addEventListener('click', function(e) {
            if (!navbarCollapse.contains(e.target) && !navbarToggler.contains(e.target)) {
                const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
                if (bsCollapse && navbarCollapse.classList.contains('show')) {
                    bsCollapse.hide();
                }
            }
        });
        
        // Cerrar menú al hacer clic en un enlace
        const navLinks = navbarCollapse.querySelectorAll('.nav-link:not(.dropdown-toggle)');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
                if (bsCollapse && navbarCollapse.classList.contains('show')) {
                    setTimeout(() => bsCollapse.hide(), 150);
                }
            });
        });
    }
    
    // Mejorar dropdowns en móvil
    const dropdownToggles = document.querySelectorAll('.dropdown-toggle');
    dropdownToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            if (window.innerWidth <= 767) {
                e.preventDefault();
                const dropdownMenu = this.nextElementSibling;
                if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
                    dropdownMenu.classList.toggle('show');
                }
            }
        });
    });
    
    // Mejorar submenus en móvil
    const submenuToggles = document.querySelectorAll('.dropdown-submenu .dropdown-toggle');
    submenuToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            if (window.innerWidth <= 767) {
                e.preventDefault();
                e.stopPropagation();
                const submenu = this.nextElementSibling;
                if (submenu) {
                    submenu.classList.toggle('show');
                }
            }
        });
    });
    
    // Optimizar imágenes lazy loading
    const images = document.querySelectorAll('img[data-src]');
    if (images.length > 0) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    observer.unobserve(img);
                }
            });
        });
        
        images.forEach(img => imageObserver.observe(img));
    }
    
    // Mejorar tablas responsive
    const tables = document.querySelectorAll('table:not(.table-responsive table)');
    tables.forEach(table => {
        if (!table.parentElement.classList.contains('table-responsive')) {
            const wrapper = document.createElement('div');
            wrapper.className = 'table-responsive';
            table.parentNode.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        }
    });
    
    // Smooth scroll para anchor links
    const anchorLinks = document.querySelectorAll('a[href^="#"]');
    anchorLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            const targetId = this.getAttribute('href');
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                e.preventDefault();
                const offsetTop = targetElement.offsetTop - 80; // Account for navbar height
                
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });
    
    // Mejorar performance de scroll en móvil
    let ticking = false;
    
    function updateScrollEffects() {
        // Actualizar efectos de scroll aquí si es necesario
        ticking = false;
    }
    
    function requestTick() {
        if (!ticking) {
            requestAnimationFrame(updateScrollEffects);
            ticking = true;
        }
    }
    
    window.addEventListener('scroll', requestTick, { passive: true });
    
    // Detectar orientación en móvil
    function handleOrientationChange() {
        // Pequeño delay para permitir que el viewport se ajuste
        setTimeout(() => {
            window.dispatchEvent(new Event('resize'));
        }, 100);
    }
    
    window.addEventListener('orientationchange', handleOrientationChange);
    
    // Prevenir zoom en inputs (iOS)
    const inputs = document.querySelectorAll('input, select, textarea');
    inputs.forEach(input => {
        input.addEventListener('focus', function() {
            if (window.innerWidth <= 767 && /iPad|iPhone|iPod/.test(navigator.userAgent)) {
                this.style.fontSize = '16px';
            }
        });
        
        input.addEventListener('blur', function() {
            this.style.fontSize = '';
        });
    });
    
    // Mejorar rendimiento de hover en touch devices
    if ('ontouchstart' in window) {
        document.body.classList.add('touch-device');
    }
});

// Utilidades responsive
window.ResponsiveUtils = {
    isMobile: () => window.innerWidth <= 767,
    isTablet: () => window.innerWidth > 767 && window.innerWidth <= 991,
    isDesktop: () => window.innerWidth > 991,
    
    // Ejecutar función según breakpoint
    onBreakpoint: function(mobile, tablet, desktop) {
        if (this.isMobile() && mobile) mobile();
        else if (this.isTablet() && tablet) tablet();
        else if (this.isDesktop() && desktop) desktop();
    }
};