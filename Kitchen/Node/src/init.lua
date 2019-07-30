station_cfg={}
station_cfg.ssid="Gunny"
station_cfg.pwd="kippukool"
wifi.setmode(wifi.STATION)
wifi.sta.config(station_cfg)
wifi.sta.connect()
tmr.alarm(1, 1000, 1, function()
    if wifi.sta.getip() == nil then
        print("IP unavailable, Waiting...")
    else
        tmr.stop(1)
        print("ESP8266 mode is: " .. wifi.getmode())
        print("The module MAC address is: " .. wifi.ap.getmac())
        print("Config done, IP is ".. wifi.sta.getip())
    end
end)

-- udpSocket = net.createUDPSocket()
-- udpSocket:listen(5000)
-- udpSocket:on("receive", function(s, data, port, ip)
--     print(string.format("received '%s' from %s:%d", data, ip, port))
--     s:send(port, ip, "echo: " .. data)
-- end)
-- port, ip = udpSocket:getaddr()
-- print(string.format("local UDP socket address / port: %s:%d", ip, port))