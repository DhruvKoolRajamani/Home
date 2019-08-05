// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
"use strict";

$(document).ready(function () {

    class Switch {
        constructor(a, b, c) {
            if (a instanceof Switch || a instanceof Object) {
                this.name = a.name;
                this.id = a.id;
                this.state = a.state;
            }
            else if (typeof (a) === 'string') {
                this.name = a;
                this.id = b;
                this.state = c;
            }
            else {
                console.log("Error");
            }
        }
    }

    var _livingRoomLeftLightOn = $('#living_room_left_on');
    var _livingRoomLeftLightOff = $('#living_room_left_off');

    var _livingRoomRightLightOn = $('#living_room_right_on');
    var _livingRoomRightLightOff = $('#living_room_right_off');

    var _livingRoomLeftLight = new Switch("Living Room", 0x00, false);
    var _livingRoomRightLight = new Switch("Living Room", 0x01, false);

    let connection = new signalR.HubConnectionBuilder()
        .withUrl("/lightHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    async function start() {
        try {
            await connection.start();
            console.log('connected');
        } catch (err) {
            console.log(err);
            setTimeout(() => start(), 5000);
        }
    };

    connection.onclose(async () => {
        await start();
    });

    connection.start()
        .then(() => onStarted())
        .catch(function (err) {
            return console.error(err.toString());
        });

    connection.on("lightStates", function (livingRoomLights, kitchenLights) {
        console.log("Entering get states");
        _livingRoomLeftLight = Object.assign(new Switch(Object), livingRoomLights[0]); // new Switch(livingRoomLights[0].name, livingRoomLights[0].id, livingRoomLights[0].state);
        _livingRoomRightLight = Object.assign(new Switch(Object), livingRoomLights[1]); // new Switch(livingRoomLights[1].name, livingRoomLights[1].id, livingRoomLights[1].state);

        if (_livingRoomLeftLight.state)
            document.getElementById("living_room_left_status").style.background = "green";
        else
            document.getElementById("living_room_left_status").style.background = "red";

        if (_livingRoomRightLight.state)
            document.getElementById("living_room_right_status").style.background = "green";
        else
            document.getElementById("living_room_right_status").style.background = "red";

        console.log(_livingRoomLeftLight, _livingRoomRightLight);
    });

    $(_livingRoomLeftLightOn).click(function () {
        console.log("Sending true");
        connection.invoke("setLightsState", _livingRoomLeftLight.name, _livingRoomLeftLight.id, true);
    });

    $(_livingRoomLeftLightOff).click(function () {
        console.log("Sending false");
        connection.invoke("setLightsState", _livingRoomLeftLight.name, _livingRoomLeftLight.id, false);
    });

    $(_livingRoomRightLightOn).click(function () {
        console.log("Sending true");
        connection.invoke("setLightsState", _livingRoomRightLight.name, _livingRoomRightLight.id, true);
    });

    $(_livingRoomRightLightOff).click(function () {
        console.log("Sending false");
        connection.invoke("setLightsState", _livingRoomRightLight.name, _livingRoomRightLight.id, false);
    });

    function onStarted() {
        connection.invoke("getLightsStates").catch(function (err) {
            return console.error(err.toString());
        });
    }
});