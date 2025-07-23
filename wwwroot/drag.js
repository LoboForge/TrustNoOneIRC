window.hackerWindowInterop = (function () {
    let currentZ = 100;

    return {
        startDrag: function (el, startX, startY) {
            el = el instanceof HTMLElement ? el : el instanceof Element ? el : el instanceof Object ? el : el.getBoundingClientRect();

            const rect = el.getBoundingClientRect();
            const offsetX = startX - rect.left;
            const offsetY = startY - rect.top;

            function onMouseMove(e) {
                el.style.left = (e.clientX - offsetX) + 'px';
                el.style.top = (e.clientY - offsetY) + 'px';
            }

            function onMouseUp() {
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
            }

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        },

        startResize: function (el, startX, startY) {
            const rect = el.getBoundingClientRect();
            const startWidth = rect.width;
            const startHeight = rect.height;

            function onMouseMove(e) {
                el.style.width = (startWidth + (e.clientX - startX)) + 'px';
                el.style.height = (startHeight + (e.clientY - startY)) + 'px';
            }

            function onMouseUp() {
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
            }

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        },

        bringToFront: function (el) {
            currentZ += 1;
            el.style.zIndex = currentZ;
        }
    };
})();
