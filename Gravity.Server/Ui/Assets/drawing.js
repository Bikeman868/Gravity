function init(evt) {
    if (window.svgDocument == null) {
        svgDocument = evt.target.ownerDocument;
    }
}

function ShowPopup(evt, id) {
    var elements = document.getElementsByClassName(id);
    for (var item of elements) {
      item.setAttributeNS(null, "visibility", "visible");
    }
}

function HidePopup(evt, id) {
    var elements = document.getElementsByClassName(id);
    for (var item of elements) {
        item.setAttributeNS(null, "visibility", "hidden");
    }
}

function Navigate(url) {
    window.location = url;
}
