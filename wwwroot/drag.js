window.hackerWindowInterop = (function () {
    let currentZ = 100;

    return {
        startDrag: function (el, startX, startY, dotNetHelper) {
            el = el instanceof HTMLElement ? el : el instanceof Element ? el : el instanceof Object ? el : el.getBoundingClientRect();

            const rect = el.getBoundingClientRect();
            const offsetX = startX - rect.left;
            const offsetY = startY - rect.top;

            function onMouseMove(e) {
                const left = e.clientX - offsetX;
                const top = e.clientY - offsetY;

                el.style.left = left + 'px';
                el.style.top = top + 'px';

                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('UpdateWindowPosition', top, left);
                }
            }

            function onMouseUp() {
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
            }

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        },

        startResize: function (el, startX, startY, dotNetHelper) {
            const rect = el.getBoundingClientRect();
            const startWidth = rect.width;
            const startHeight = rect.height;

            function onMouseMove(e) {
                const newWidth = startWidth + (e.clientX - startX);
                const newHeight = startHeight + (e.clientY - startY);

                el.style.width = newWidth + 'px';
                el.style.height = newHeight + 'px';

                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('UpdateWindowSize', newWidth, newHeight);
                }
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
