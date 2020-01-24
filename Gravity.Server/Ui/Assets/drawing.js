function init(evt) {
    if (window.svgDocument == null) {
        svgDocument = evt.target.ownerDocument;
    }
}

function showPopup(evt, id) {
    var elements = document.getElementsByClassName(id);
    for (var item of elements) {
      item.setAttributeNS(null, "visibility", "visible");
    }
}

function hidePopup(evt, id) {
    var elements = document.getElementsByClassName(id);
    for (var item of elements) {
        item.setAttributeNS(null, "visibility", "hidden");
    }
}
