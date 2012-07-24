<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="JsonValueSample.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>JsonValue sample</title>
        <link href="main.css" rel="stylesheet" type="text/css" />
    <link href="Scripts/jquery-ui.css"
        rel="stylesheet" type="text/css" />
    <script src="Scripts/jquery-1.5.2.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery-ui-1.8.9.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.tmpl.min.js"
        type="text/javascript"></script>
    <script type="text/javascript">
        $(function () {
            $("#addContact").submit(function () {
                $.post(
                    "contacts",
                    $("#addContact").serialize(),
                    function (value) {
                        alert(
                          'Contact ' + value.Name + ' created, ContactId = ' + value.ContactId
                        );
                    },
                    "json"
                );
                return false;
            });
            $("body").addClass("ui-widget");
        });
    </script>
</head>
<body>
    <form action="contacts/" method="post" id="addContact">
    <fieldset>
        <legend>Add New Contact</legend>
        <ol>
            <li>
                <label for="Name">
                    Name</label>
                <input type="text" name="Name" />
            </li>
        </ol>
        <input type="submit" value="Add" />
    </fieldset>
    </form>
</body>
</html>
