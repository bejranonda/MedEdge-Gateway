// MudBlazor JavaScript Interop
// This file provides the necessary JavaScript functionality for MudBlazor components

window.mudBlazor = {
    getElement: function (element) {
        if (typeof element === 'string') {
            return document.querySelector(element);
        }
        return element;
    },

    getBoundingClientRect: function (element) {
        var el = this.getElement(element);
        if (!el) {
            return { left: 0, top: 0, width: 0, height: 0, right: 0, bottom: 0 };
        }
        return el.getBoundingClientRect();
    },

    setElementProperty: function (element, property, value) {
        var el = this.getElement(element);
        if (el) {
            el[property] = value;
        }
    },

    addElementClass: function (element, className) {
        var el = this.getElement(element);
        if (el) {
            el.classList.add(className);
        }
    },

    removeElementClass: function (element, className) {
        var el = this.getElement(element);
        if (el) {
            el.classList.remove(className);
        }
    },

    addElementEventListener: function (element, eventName, callback, useCapture) {
        var el = this.getElement(element);
        if (el) {
            el.addEventListener(eventName, callback, useCapture || false);
        }
    },

    addElementStyle: function (element, key, value) {
        var el = this.getElement(element);
        if (el) {
            el.style[key] = value;
        }
    },

    createProxy: function (element) {
        return {
            getBoundingClientRect: () => this.getBoundingClientRect(element),
            addEventListener: (eventName, handler, options) => this.addElementEventListener(element, eventName, handler, options),
            appendChild: (child) => {
                if (element.appendChild) {
                    return element.appendChild(child);
                }
            },
            setAttribute: (name, value) => {
                if (element.setAttribute) {
                    element.setAttribute(name, value);
                }
            }
        };
    }
};

// MudBlazor Resize Listener
window.mudResizeListener = {
    windowSize: {
        width: window.innerWidth || 0,
        height: window.innerHeight || 0
    },

    listeners: [],

    getBrowserWindowSize: function () {
        return {
            width: window.innerWidth || 0,
            height: window.innerHeight || 0
        };
    },

    addResizeListener: function (callback) {
        var index = this.listeners.indexOf(callback);
        if (index === -1) {
            this.listeners.push(callback);
        }
        if (this.listeners.length === 1) {
            window.addEventListener('resize', this.handleResize.bind(this));
        }
    },

    removeResizeListener: function (callback) {
        var index = this.listeners.indexOf(callback);
        if (index !== -1) {
            this.listeners.splice(index, 1);
        }
        if (this.listeners.length === 0) {
            window.removeEventListener('resize', this.handleResize.bind(this));
        }
    },

    handleResize: function () {
        var newSize = {
            width: window.innerWidth,
            height: window.innerHeight
        };

        // Only trigger if size actually changed
        if (newSize.width !== this.windowSize.width || newSize.height !== this.windowSize.height) {
            this.windowSize = newSize;
            this.listeners.forEach(function (callback) {
                try {
                    callback(newSize);
                } catch (e) {
                    console.error('Error in resize listener:', e);
                }
            });
        }
    }
};

// Backwards compatibility with older MudBlazor versions
if (window.mudElementRef === undefined) {
    window.mudElementRef = {
        getBoundingClientRect: function (element) {
            return window.mudBlazor ? window.mudBlazor.getBoundingClientRect(element) : { left: 0, top: 0, width: 0, height: 0 };
        }
    };
}
