// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
"use strict";

function onRangeChange(rangeInputElmt, listener) {

    var inputEvtHasNeverFired = true;

    var rangeValue = { current: undefined, mostRecent: undefined };

    rangeInputElmt.addEventListener("input", function (evt) {
        inputEvtHasNeverFired = false;
        rangeValue.current = evt.target.value;
        if (rangeValue.current !== rangeValue.mostRecent) {
            listener(evt);
        }
        rangeValue.mostRecent = rangeValue.current;
    });

    rangeInputElmt.addEventListener("change", function (evt) {
        if (inputEvtHasNeverFired) {
            listener(evt);
        }
    });
}

$(document).ready(function () {

    changeWeather(weather[Math.random() * 3])

    var _upperTankPumpOn = $("#upper_tank_pump_on");
    var _upperTankPumpOff = $("#upper_tank_pump_off");

    var _lowerTankPumpOn = $('#lower_tank_pump_on');
    var _lowerTankPumpOff = $('#lower_tank_pump_off');

    // var _upperTankMusicStop = $('#upper_tank_music_stop');
    // var _lowerTankMusicStop = $('#lower_tank_music_stop');

    var _ventOn = $("#vent_on");
    var _ventOff = $("#vent_off");

    var _tempDiv = $("#temperature.temp");
    console.log("NOW: " + _tempDiv.html());
    var _humDiv = $("#humidity");

    var _temp = 0.0;
    var _humidity = 0.0;

    var _upperTankSoundState = true;
    var _lowerTankSoundState = true;

    var _upperTankDepth = 0.0;
    var _lowerTankDepth = 0.0;

    var _upperTankPumpState = false;
    var _lowerTankPumpState = false;

    var _ventState = false;
    var _ventSpeed = 50;
    var _ventCalibrationState = false;

    var slider = document.getElementById("ventRange");
    var output = document.getElementById("ventValue");

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

    connection.on("ventStatus", function (ventState, ventSpeed, ventCalibrationState) {
        _ventState = ventState;
        _ventSpeed = ventSpeed;
        _ventCalibrationState = ventCalibrationState;

        if (_ventState)
            document.getElementById("vent_status").style.background = "green";
        else
            document.getElementById("vent_status").style.background = "red";

        if (ventSpeed >= 0 && ventSpeed <= 100)
            slider.value = ventSpeed;

        console.log('Vent State', _ventState);
        console.log('Vent Speed', _ventSpeed);
        console.log('Vent Calibration State', _ventCalibrationState);
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

    connection.on("weatherData", function (temp, humidity) {
        _temp = temp;
        _humidity = humidity;

        if (_humidity > 80)
            changeWeather(weather[2]);
        else if (_humidity > 95)
            changeWeather(weather[3]);
        else
            changeWeather(weather[0]);
        var currentdate = new Date();
        var dt = currentdate.getDate().toString() + "/" + (currentdate.getMonth()).toString(); /*+ "/" + currentdate.getFullYear().toString();*/
        _tempDiv.html(
            _temp.toString() + "<span>c</span></div>" + 
            "<div class=\"right\"><div id=\"date\">" + dt + 
            "</div><div id=\"summary\">" + currentWeather.name + 
            "</div></div>");

        // console.log("Received Humidity " + _humidity + " and temperature " + _temp);
    });

    connection.on("onNotification", function (message) {
        console.log(message);
    });

    connection.on("levels", function (upperLevel, lowerLevel) {
        _upperTankDepth = upperLevel * 100;
        _lowerTankDepth = lowerLevel * 100;

        if (upperLevel >= 1) {
            _upperTankDepth = 100;

            // if (_upperTankSoundState) {
            //     _upperTankSound.play();
            // }
        }

        if (lowerLevel >= 1) {
            _lowerTankDepth = 100;

            // if (_lowerTankSoundState) {
            //     _lowerTankSound.play();
            // }
        }

        if (upperLevel <= 0)
            _upperTankDepth = 0;

        if (lowerLevel <= 0)
            _lowerTankDepth = 0;

        document.getElementById("upper_tank").style.height = _upperTankDepth.toString() + "%";
        document.getElementById("lower_tank").style.height = _lowerTankDepth.toString() + "%";

        console.log('Getting Levels: ' + _upperTankDepth + " : " + _lowerTankDepth);
    });

    function ventCommand(state = false, speed = 50) {
        _ventSpeed = speed;
        _ventState = state;

        console.log("Vent State: ", _ventState);
        console.log("Setting Default Speed: ", _ventSpeed);
        console.log("Calibration State: ", _ventCalibrationState);

        connection.invoke("setVentState", Boolean(_ventState), Number(_ventSpeed), Boolean(_ventCalibrationState));
    }

    var myNumEvts = { input: 0, change: 0, custom: 0 };

    ["input", "change"].forEach(function (myEvtType) {
        slider.addEventListener(myEvtType, function () {
            myNumEvts[myEvtType] += 1;
        });
    });

    var myListener = function (myEvt) {
        myNumEvts["custom"] += 1;
        output.innerHTML = "range value: " + myEvt.target.value;
        ventCommand(true, myEvt.target.value);
    };

    onRangeChange(slider, myListener);

    // var _upperTankSound = new Howl({
    //     src: ['./audio/alert1.mp3'],
    //     volume: 0.7,
    //     onend: function () {
    //         console.log('Finished!');
    //     }
    // });

    // var _lowerTankSound = new Howl({
    //     src: ['./audio/alert.mp3'],
    //     volume: 0.7,
    //     onend: function () {
    //         console.log('Finished!');
    //     }
    // });

    // output.innerHTML = slider.value;

    // slider.oninput = function () {
    //     output.innerHTML = this.value;
    // }

    $(_ventOn).click(function () {
        console.log('Vent ON');
        ventCommand(true);
    });

    $(_ventOff).click(function () {
        console.log('Vent OFF');
        ventCommand(false, 0);
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

        // _upperTankSound.stop();
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

        // _lowerTankSound.stop();
        connection.invoke("setLowerTankPumpState", _lowerTankPumpState);
    });

    // $(_upperTankMusicStop).click(function () {
    //     _upperTankSoundState = false;
    //     _upperTankSound.stop();
    // });

    // $(_lowerTankMusicStop).click(function () {
    //     _lowerTankSoundState = false;
    //     _lowerTankSound.stop();
    // });

    function onStarted() {

        connection.invoke("getTankLevels").catch(function (err) {
            return console.error(err.toString());
        });

        connection.invoke("getPumpStates").catch(function (err) {
            return console.error(err.toString());
        });

        connection.invoke("getVentState").catch(function (err) {
            return console.error(err.toString());
        });

        connection.invoke("sendWeatherData").catch(function (err) {
            return console.error(err.toString());
        });
    }
});