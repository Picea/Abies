// wwwroot/js/pine.js

import { dotnet } from './_framework/dotnet.js';

const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(true)
    .withConfig({
        environmentVariables: {
            "MONO_LOG_LEVEL": "debug", //enable Mono VM detailed logging by
            "MONO_LOG_MASK": "all", // categories, could be also gc,aot,type,...
        }
    })
    .create();

const registeredEvents = new Set();

function ensureEventListener(eventName) {
    if (registeredEvents.has(eventName)) return;
    document.body.addEventListener(eventName, genericEventHandler);
    registeredEvents.add(eventName);
}

function genericEventHandler(event) {
    const name = event.type;
    const target = event.target.closest(`[data-event-${name}]`);
    if (target) {
        const message = target.getAttribute(`data-event-${name}`);
        if (message) {
            const data = buildEventData(event, target);
            exports.Abies.Runtime.DispatchData(message, JSON.stringify(data));
            if (name === 'click') {
                event.preventDefault();
            }
        } else {
            console.error(`No message id found in data-event-${name} attribute.`);
        }
    }
}

function buildEventData(event, target) {
    const data = {};
    if (target && 'value' in target) data.value = target.value;
    if (target && 'checked' in target) data.checked = target.checked;
    if ('key' in event) {
        data.key = event.key;
        data.altKey = event.altKey;
        data.ctrlKey = event.ctrlKey;
        data.shiftKey = event.shiftKey;
    }
    if ('clientX' in event) {
        data.clientX = event.clientX;
        data.clientY = event.clientY;
        data.button = event.button;
    }
    return data;
}

function addEventListeners() {
    document.querySelectorAll('*').forEach(el => {
        for (const attr of el.attributes) {
            if (attr.name.startsWith('data-event-')) {
                const name = attr.name.substring('data-event-'.length);
                ensureEventListener(name);
            }
        }
    });
}

setModuleImports('abies.js', {

    /**
     * Adds a child element to a parent element in the DOM using HTML content.
     * @param {number} parentId - The ID of the parent element.
     * @param {string} childHtml - The HTML string of the child element to add.
     */
    addChildHtml: async (parentId, childHtml) => {
        const parent = document.getElementById(parentId);
        if (parent) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = childHtml;
            const childElement = tempDiv.firstElementChild;
            parent.appendChild(childElement);
            addEventListeners(); // Reattach event listeners to new elements
        } else {
            console.error(`Parent element with ID ${parentId} not found.`);
        }
    },


    /**
     * Sets the title of the document.
     * @param {string} title - The new title of the document.
     */
    setTitle: async (title) => {
        document.title = title;
    },

    /**
     * Removes a child element from the DOM.
     * @param {number} parentId - The ID of the parent element.
     * @param {number} childId - The ID of the child element to remove.
     */
    removeChild: async (parentId, childId) =>  {
        const parent = document.getElementById(parentId);
        const child = document.getElementById(childId);
        if (parent && child && parent.contains(child)) {
            parent.removeChild(child);
        } else {
            console.error(`Cannot remove child with ID ${childId} from parent with ID ${parentId}.`);
        }
    },

    /**
     * Replaces an existing node with new HTML content.
     * @param {number} oldNodeId - The ID of the node to replace.
     * @param {string} newHtml - The HTML string to replace with.
     */
    replaceChildHtml: async (oldNodeId, newHtml) => {
        const oldNode = document.getElementById(oldNodeId);
        if (oldNode && oldNode.parentNode) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = newHtml;
            const newElement = tempDiv.firstElementChild;
            oldNode.parentNode.replaceChild(newElement, oldNode);
            addEventListeners(); // Reattach event listeners to new elements
        } else {
            console.error(`Node with ID ${oldNodeId} not found or has no parent.`);
        }
    },

    /**
     * Updates the text content of a DOM element.
     * @param {number} nodeId - The ID of the node to update.
     * @param {string} newText - The new text content.
     */
    updateTextContent: async (nodeId, newText) => {
        const node = document.getElementById(nodeId);
        if (node) {
            node.textContent = newText;
        } else {
            console.error(`Node with ID ${nodeId} not found.`);
        }
    },

    /**
     * Updates or adds an attribute of a DOM element.
     * @param {number} nodeId - The ID of the node to update.
     * @param {string} propertyName - The name of the attribute/property.
     * @param {string} propertyValue - The new value for the attribute/property.
     */
    updateAttribute: async (nodeId, propertyName, propertyValue) => {
        const node = document.getElementById(nodeId);
        if (node) {
            node.setAttribute(propertyName, propertyValue);
            if (propertyName.startsWith('data-event-')) {
                const name = propertyName.substring('data-event-'.length);
                ensureEventListener(name);
            }
        } else {
            console.error(`Node with ID ${nodeId} not found.`);
        }
    },

    addAttribute: async (nodeId, propertyName, propertyValue) => {
        const node = document.getElementById(nodeId);
        if (node) {
            node.setAttribute(propertyName, propertyValue);
            if (propertyName.startsWith('data-event-')) {
                const name = propertyName.substring('data-event-'.length);
                ensureEventListener(name);
            }
        } else {
            console.error(`Node with ID ${nodeId} not found.`);
        }
    },

    /**
     * Removes an attribute/property from a DOM element.
     * @param {number} nodeId - The ID of the node to update.
     * @param {string} propertyName - The name of the attribute/property to remove.
     */
    removeAttribute: async (nodeId, propertyName) =>{
        const node = document.getElementById(nodeId);
        if (node) {
            node.removeAttribute(propertyName);
        } else {
            console.error(`Node with ID ${nodeId} not found.`);
        }
    },

    getValue: (id) => {
        const el = document.getElementById(id);
        return el ? el.value : null;
    },

    /**
     * Sets the inner HTML of the 'app' div.
     * @param {string} html - The HTML content to set.
     */
    setAppContent: async (html) => {
        document.body.innerHTML = html;
        addEventListeners(); // Ensure event listeners are attached
    },

    // Expose functions to .NET via JS interop (if needed)
    getCurrentUrl: () => {
        return window.location.href;
    },

    pushState: async (url) => {
        history.pushState(null, "", url);
    },

    replaceState: async (url) => {
        history.replaceState(null, "", url);
    },

    back: async (x) => {
        history.go(-x);
    },

    forward: async (x) => {
        history.go(x);
    },

    go: async (x) => {
        history.go(x);
    },

    load: async (url) => {
        window.location.reload(url);
    },

    reload: async () => {
        window.location.reload();
    },

    onUrlChange: (callback) => {
        window.addEventListener("popstate", () => callback(getCurrentUrl()));
    },

    onFormSubmit: (callback) => {
        document.addEventListener("submit", (event) => {
            event.preventDefault();
            const form = event.target;
            callback(form.action);
        });
    },

    onLinkClick: (callback) => {
        document.addEventListener("click", (event) => {
            const link = event.target.closest("a");
            if (link) {
                event.preventDefault();
                callback(link.href);
            }
        });
    }


});

const config = getConfig();
const exports = await getAssemblyExports("Abies");

await runMain(); // Ensure the .NET runtime is initialized