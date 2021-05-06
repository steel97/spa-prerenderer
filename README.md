# SPA-Prerenderer
This project aimed to help with SEO optimization for those who forget about SSR


# How it works?
1) Serve your original SPA content via builtin asp.net core kestrel server
2) Prerender your pages in background with [puppeteer-sharp](https://github.com/hardkoded/puppeteer-sharp) and cache them to filesystem or memory
3) Serve prerendered content for crawlers and original content for users

# Prerequisites
1) linux or windows host
2) (linux only) **libnss3 libgbm1**
```sudo apt-get install libgbm1 gconf-service libasound2 libatk1.0-0 libc6 libcairo2 libcups2 libdbus-1-3 libexpat1 libfontconfig1 libgcc1 libgconf-2-4 libgdk-pixbuf2.0-0 libglib2.0-0 libgtk-3-0 libnspr4 libpango-1.0-0 libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 libxrender1 libxss1 libxtst6 ca-certificates fonts-liberation libappindicator1 libnss3 lsb-release xdg-utils wget libxkbcommon-x11-0 libxss1 libxshmfence1```
3) (optional) nginx as kestrel proxy


# Setup

Download latest release or clone github repository
1) unpack downloaded release
2) configure server via **appsettings.json**
3) run release on linux via
```./spa-prerenderer```
or on windows
```spa-prerenderer.exe```
4) (optional) configure nginx proxy
5) (optional) install as systemd service
6) if you choose to use sources you can run server with 
```dotnet run```


# Configuration


# Licenses
1) Project uses [puppeteer-sharp](https://github.com/hardkoded/puppeteer-sharp) licensed under **MIT**
1) Project uses [detection](https://github.com/wangkanai/Detection) licensed under **Apache License 2.0**