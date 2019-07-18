// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
"use strict";

$(document).ready(function () {

    var _upperTankPumpOn = $("#upper_tank_pump_on");
    var _upperTankPumpOff = $("#upper_tank_pump_off");

    var _lowerTankPumpOn = $('#lower_tank_pump_on');
    var _lowerTankPumpOff = $('#lower_tank_pump_off');

    var _upperTankMusicStop = $('#upper_tank_music_stop');
    var _lowerTankMusicStop = $('#lower_tank_music_stop');

    var _upperTankSoundState = true;
    var _lowerTankSoundState = true;

    var _upperTankDepth = 0.0;
    var _lowerTankDepth = 0.0;

    var _upperTankPumpState = false;
    var _lowerTankPumpState = false;

    var slider = document.getElementById("ventRange");
    var output = document.getElementById("ventValue");

    var _upperTankSound = new Howl({
        src: ['./audio/alert1.mp3'],
        volume: 0.7,
        onend: function () {
            console.log('Finished!');
        }
    });

    var _lowerTankSound = new Howl({
        src: ['./audio/alert.mp3'],
        volume: 0.7,
        onend: function () {
            console.log('Finished!');
        }
    });

    output.innerHTML = slider.value;

    slider.oninput = function () {
        output.innerHTML = this.value;
    }

    let connection = new signalR.HubConnectionBuilder()
        .withUrl("/kitchen")
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

    connection.on("upperTankPumpStatus", function (upperTankPumpState) {
        _upperTankPumpState = upperTankPumpState;

        if (_upperTankPumpState)
            document.getElementById("upper_tank_status").style.background = "green";
        else
            document.getElementById("upper_tank_status").style.background = "red";
        console.log('Upper Tank State: ', _lowerTankPumpState);
    });

    connection.on("lowerTankPumpStatus", function (lowerTankPumpState) {
        _lowerTankPumpState = lowerTankPumpState;

        if (_lowerTankPumpState)
            document.getElementById("lower_tank_status").style.background = "green";
        else
            document.getElementById("lower_tank_status").style.background = "red";
        console.log('Lower Tank State: ', _lowerTankPumpState);
    });

    connection.on("onNotification", function (message) {
        console.log(message);
    });

    connection.on("levels", function (upperLevel, lowerLevel) {
        _upperTankDepth = upperLevel * 100;
        _lowerTankDepth = lowerLevel * 100;

        if (upperLevel >= 1) {
            _upperTankDepth = 100;

            if (_upperTankSoundState) {
                _upperTankSound.play();
            }
        }

        if (lowerLevel >= 1) {
            _lowerTankDepth = 100;

            if (_lowerTankSoundState) {
                _lowerTankSound.play();
            }
        }

        if (upperLevel <= 0)
            _upperTankDepth = 0;

        if (lowerLevel <= 0)
            _lowerTankDepth = 0;

        document.getElementById("upper_tank").style.height = _upperTankDepth.toString() + "%";
        document.getElementById("lower_tank").style.height = _lowerTankDepth.toString() + "%";

        console.log('Getting Levels: ' + _upperTankDepth + " : " + _lowerTankDepth);
    });

    function onStarted() {

        connection.invoke("getTankLevels").catch(function (err) {
            return console.error(err.toString());
        });

        connection.invoke("getPumpStates").catch(function (err) {
            return console.error(err.toString());
        });

        $(_upperTankPumpOn).click(function () {
            _upperTankPumpState = true;
            _upperTankSoundState = true;
            console.log("Upper Tank Pump State: ", _upperTankPumpState);

            connection.invoke("setUpperTankPumpState", _upperTankPumpState);
        });

        $(_upperTankPumpOff).click(function () {
            _upperTankPumpState = false;
            _upperTankSoundState = false;
            console.log("Upper Tank Pump State: ", _upperTankPumpState);

            _upperTankSound.stop();
            connection.invoke("setUpperTankPumpState", _upperTankPumpState);
        });

        $(_lowerTankPumpOn).click(function () {
            _lowerTankPumpState = true;
            _lowerTankSoundState = true;
            console.log("Lower Tank Pump State: ", _lowerTankPumpState);

            connection.invoke("setLowerTankPumpState", _lowerTankPumpState);
        });

        $(_lowerTankPumpOff).click(function () {
            _lowerTankPumpState = false;
            _lowerTankSoundState = false;
            console.log("Lower Tank Pump State: ", _lowerTankPumpState);

            _lowerTankSound.stop();
            connection.invoke("setLowerTankPumpState", _lowerTankPumpState);
        });

        $(_upperTankMusicStop).click(function () {
            _upperTankSoundState = false;
            _upperTankSound.stop();
        })

        $(_lowerTankMusicStop).click(function () {
            _lowerTankSoundState = false;
            _lowerTankSound.stop();
        })
    }
});