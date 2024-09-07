window.virtualScrolling = {
    getScrollPosition: function (element) {
        if (element) {
            return element.scrollTop || 0;
        }
        return 0;
    },

    scrollToBottom: function (element) {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },

    isScrolledToBottom: function (element) {
        if (element) {
            return element.scrollHeight - element.scrollTop === element.clientHeight;
        }
        return false;
    },

    getElementHeight: function (elementId) {
        var element = document.getElementById(elementId);
        if (element) {
            var rect = element.getBoundingClientRect();
            var computedStyle = getComputedStyle(element);
            var marginTop = parseFloat(computedStyle.marginTop);
            var marginBottom = parseFloat(computedStyle.marginBottom);
            var totalHeight = rect.height + marginTop + marginBottom; // Учитываем margin и фактическую высоту

            return totalHeight; // Возвращаем общую высоту компонента
        }
        return 0;
    },

    getElementPosition: function (elementId) {
        var element = document.getElementById(elementId);
        if (element) {
            return element.getBoundingClientRect().top;
        }
        return 0;
    },

    getContainerHeight: function (element) {
        if (element) {
            return element.scrollHeight;
        }
        return 0;
    }
};
