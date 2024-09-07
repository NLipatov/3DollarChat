"use strict";

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
            return element.offsetHeight;
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
