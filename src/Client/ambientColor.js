// Ambient Color Extraction for Projector Backdrop
// Samples colors from visible poster images and updates the projector beam color

let currentColor = { r: 255, g: 180, b: 100 }; // Default warm amber
let targetColor = { r: 255, g: 180, b: 100 };
let animationFrame = null;

/**
 * Extract the dominant color from an image using canvas sampling
 */
function extractColorFromImage(img) {
    try {
        // Create a small canvas for sampling (performance optimization)
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d', { willReadFrequently: true });

        // Sample at a small size for performance
        const sampleSize = 50;
        canvas.width = sampleSize;
        canvas.height = sampleSize;

        // Draw the image scaled down
        ctx.drawImage(img, 0, 0, sampleSize, sampleSize);

        // Get pixel data
        const imageData = ctx.getImageData(0, 0, sampleSize, sampleSize);
        const pixels = imageData.data;

        let r = 0, g = 0, b = 0;
        let count = 0;

        // Sample pixels, skipping very dark and very light ones
        for (let i = 0; i < pixels.length; i += 16) { // Sample every 4th pixel
            const pr = pixels[i];
            const pg = pixels[i + 1];
            const pb = pixels[i + 2];
            const pa = pixels[i + 3];

            // Skip transparent pixels
            if (pa < 128) continue;

            // Calculate luminance
            const luminance = (pr * 0.299 + pg * 0.587 + pb * 0.114);

            // Skip very dark (< 30) and very light (> 225) pixels
            if (luminance < 30 || luminance > 225) continue;

            // Weight by saturation - more saturated colors are more interesting
            const max = Math.max(pr, pg, pb);
            const min = Math.min(pr, pg, pb);
            const saturation = max === 0 ? 0 : (max - min) / max;
            const weight = 0.5 + saturation * 0.5;

            r += pr * weight;
            g += pg * weight;
            b += pb * weight;
            count += weight;
        }

        if (count > 0) {
            return {
                r: Math.round(r / count),
                g: Math.round(g / count),
                b: Math.round(b / count)
            };
        }
    } catch (e) {
        // Canvas may fail for cross-origin images without CORS
        console.debug('Could not extract color from image:', e.message);
    }

    return null;
}

/**
 * Boost the saturation of a color to make it more vibrant for the projector
 */
function boostColor(color) {
    // Convert to HSL
    const r = color.r / 255;
    const g = color.g / 255;
    const b = color.b / 255;

    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    let h, s, l = (max + min) / 2;

    if (max === min) {
        h = s = 0;
    } else {
        const d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);

        switch (max) {
            case r: h = ((g - b) / d + (g < b ? 6 : 0)) / 6; break;
            case g: h = ((b - r) / d + 2) / 6; break;
            case b: h = ((r - g) / d + 4) / 6; break;
        }
    }

    // Boost saturation and ensure good lightness for projector effect
    s = Math.min(1, s * 1.3 + 0.1);
    l = Math.max(0.5, Math.min(0.7, l)); // Keep lightness in a good range

    // Convert back to RGB
    function hue2rgb(p, q, t) {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1/6) return p + (q - p) * 6 * t;
        if (t < 1/2) return q;
        if (t < 2/3) return p + (q - p) * (2/3 - t) * 6;
        return p;
    }

    let newR, newG, newB;
    if (s === 0) {
        newR = newG = newB = l;
    } else {
        const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        const p = 2 * l - q;
        newR = hue2rgb(p, q, h + 1/3);
        newG = hue2rgb(p, q, h);
        newB = hue2rgb(p, q, h - 1/3);
    }

    return {
        r: Math.round(newR * 255),
        g: Math.round(newG * 255),
        b: Math.round(newB * 255)
    };
}

/**
 * Find all poster images currently visible on the page
 */
function findPosterImages() {
    // Look for images within poster cards
    const posterImages = document.querySelectorAll('.poster-image, .poster-card img, [class*="poster"] img');

    // Filter to only images that are loaded and visible
    return Array.from(posterImages).filter(img => {
        if (!img.complete || !img.naturalWidth) return false;

        // Check if image is in viewport
        const rect = img.getBoundingClientRect();
        return rect.top < window.innerHeight && rect.bottom > 0;
    });
}

/**
 * Calculate the average color from multiple images
 */
function calculateAverageColor(images) {
    const colors = [];

    for (const img of images) {
        const color = extractColorFromImage(img);
        if (color) {
            colors.push(color);
        }
    }

    if (colors.length === 0) {
        return null;
    }

    // Average all colors
    const avg = colors.reduce((acc, c) => ({
        r: acc.r + c.r,
        g: acc.g + c.g,
        b: acc.b + c.b
    }), { r: 0, g: 0, b: 0 });

    return {
        r: Math.round(avg.r / colors.length),
        g: Math.round(avg.g / colors.length),
        b: Math.round(avg.b / colors.length)
    };
}

/**
 * Smoothly interpolate between current and target color
 */
function lerpColor(current, target, t) {
    return {
        r: Math.round(current.r + (target.r - current.r) * t),
        g: Math.round(current.g + (target.g - current.g) * t),
        b: Math.round(current.b + (target.b - current.b) * t)
    };
}

/**
 * Update the CSS custom properties on the backdrop
 */
function applyColor(color) {
    const backdrop = document.querySelector('.animated-backdrop');
    if (backdrop) {
        backdrop.style.setProperty('--projector-r', color.r);
        backdrop.style.setProperty('--projector-g', color.g);
        backdrop.style.setProperty('--projector-b', color.b);
    }
}

/**
 * Animation loop for smooth color transitions
 */
function animateColor() {
    // Interpolate towards target
    const t = 0.02; // Slow, smooth transition
    currentColor = lerpColor(currentColor, targetColor, t);

    applyColor(currentColor);

    // Continue animating if not at target
    const diff = Math.abs(currentColor.r - targetColor.r) +
                 Math.abs(currentColor.g - targetColor.g) +
                 Math.abs(currentColor.b - targetColor.b);

    if (diff > 1) {
        animationFrame = requestAnimationFrame(animateColor);
    } else {
        animationFrame = null;
    }
}

/**
 * Main function to update the projector color based on visible images
 */
export function updateProjectorColor() {
    const images = findPosterImages();

    if (images.length === 0) {
        // Reset to default warm amber when no images
        targetColor = { r: 255, g: 180, b: 100 };
    } else {
        const avgColor = calculateAverageColor(images);
        if (avgColor) {
            // Boost the color for better visual effect
            targetColor = boostColor(avgColor);
        }
    }

    // Start animation if not already running
    if (!animationFrame) {
        animationFrame = requestAnimationFrame(animateColor);
    }
}

/**
 * Initialize the ambient color system with periodic updates
 */
export function initAmbientColor() {
    // Initial update
    updateProjectorColor();

    // Set up observer for DOM changes (new images loaded)
    const observer = new MutationObserver(() => {
        // Debounce updates
        clearTimeout(observer.timeout);
        observer.timeout = setTimeout(updateProjectorColor, 300);
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Also update on scroll (different images become visible)
    let scrollTimeout;
    window.addEventListener('scroll', () => {
        clearTimeout(scrollTimeout);
        scrollTimeout = setTimeout(updateProjectorColor, 150);
    }, { passive: true });

    // Update when images finish loading
    document.addEventListener('load', (e) => {
        if (e.target.tagName === 'IMG') {
            setTimeout(updateProjectorColor, 100);
        }
    }, true);

    return () => {
        observer.disconnect();
        if (animationFrame) {
            cancelAnimationFrame(animationFrame);
        }
    };
}

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initAmbientColor);
} else {
    initAmbientColor();
}
