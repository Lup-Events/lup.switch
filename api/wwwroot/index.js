var serialInput = $("#serial");
var output = $("#output");
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

function onSerialSubmit(){
    // Get and clean input
    var serial = serialInput.val().trim().toUpperCase();
    
    // TODO: reject if not 12 characters
    
    if(serial === ""){
        return;
    }
    serialInput.val("");
    add(serial);
}

function add(serial){
    
    var th = $("<th/>");
    th.text(serial);
    
    var td = $("<td/>");
    td.append($("<span class=\"text-secondary\"><i class=\"spinner-border status-icon\" role=\"status\"></i> Processing...</span>"));
    
    var row = $("<tr/>");
    row.attr("scope", "row");
    row.append(th);
    row.append(td);
    
    output.prepend(row);

    fetch('/api/Sim/' + serial,
        {
            method: "PUT",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ // TODO: serialize needed here?
                status: "active"
            })
        })
        .then(
            function(response) {
                switch(response.status){
                    case 200:
                        td.html("<i class=\"bi bi-check-circle-fill text-success\"></i> Success");
                        break;
                    case 202:
                        td.html("<i class=\"bi bi-check-circle-fill text-success\"></i> No change needed");
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