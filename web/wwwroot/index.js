var serialInput = $("#serial");
var output = $("#output");
let targetStatus="";

swapActivate();

serialInput.focus(function() {
    var $this = $(this);
    $this.select();

    // Work around Chrome's little problem
    $this.mouseup(function() {
        // Prevent further mouseup intervention
        $this.unbind("mouseup");
        return false;
    });
});
serialInput.focus();

$.qrCodeReader.jsQRpath = "/lib/qrcode-reader/dist/js/jsQR/jsQR.js";
$.qrCodeReader.beepPath = "/lib/qrcode-reader/dist/audio/beep.mp3";
$("#qr-reader").qrCodeReader({
    multiple: true,
    skipDuplicates: true,
    callback: function(codes) {
        for(var x in codes){
            add(codes[x]);
        }
    }
});

function swapActivate(){
    targetStatus="active";

    $("#title").text("Activate SIM");
    
    $("#swapActivate").removeClass("active");
    $("#swapDeactivate").removeClass("active");
    
    $("#swapActivate").addClass("active");
}

function swapDeactivate(){
    targetStatus="inactive";

    $("#title").text("Deactivate SIM");
    
    $("#swapActivate").removeClass("active");
    $("#swapDeactivate").removeClass("active");

    $("#swapDeactivate").addClass("active");
}

function onSerialSubmit(){
    var serial = serialInput.val().trim().toUpperCase();
    
    if(serial === ""){
        return;
    }
    serialInput.val("");
    add(serial);
}

function add(serial){
    // Get and clean input
    var serial = serial.trim().toUpperCase();
    
    var th = $("<th/>");
    th.text(serial);
    
    var td = $("<td/>");
    td.append($("<span class=\"text-secondary\"><i class=\"spinner-border status-icon\" role=\"status\"></i> Processing...</span>"));
    
    var row = $("<tr/>");
    row.attr("scope", "row");
    row.append(th);
    row.append(td);
    
    output.prepend(row);

    var localTargetStatus=targetStatus;
    fetch('/api/Sim/' + serial,
        {
            method: "PUT",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ // TODO: serialize needed here?
                status: localTargetStatus
            })
        })
        .then(
            function(response) {
                switch(response.status){
                    case 200:
                        td.html("<i class=\"bi bi-check-circle-fill text-success\"></i> Success - SIM now "+localTargetStatus);
                        break;
                    case 202:
                        td.html("<i class=\"bi bi-check-circle-fill text-success\"></i> No change, already "+localTargetStatus);
                        break;
                    case 404:
                        td.html("<i class=\"bi bi-exclamation-circle-fill text-danger\"></i> SIM not found");
                        break;
                    default:
                        td.html("<i class=\"bi bi-exclamation-circle-fill text-danger\"></i> Unexpected response: " + response.status);
                        break;
                }
            }
        )
        .catch(function(error) {
            td.html("<i class=\"bi bi-exclamation-circle-fill text-danger\"></i> " + error);
        });
}