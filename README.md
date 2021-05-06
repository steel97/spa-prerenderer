# SPA-Prerenderer
This project aimed to help with SEO optimization for those who forget about SSR


# How it works?
1) Serve your original SPA content via builtin asp.net core kestrel server
2) Prerender your pages in background with [puppeteer-sharp](https://github.com/hardkoded/puppeteer-sharp) and cache them to filesystem or memory
3) Serve prerendered content for crawlers and original content for users

# Prerequisites
1) linux or windows host
2) (linux only) **libnss3 libgbm1**
```sudo apt-get install libnss3 libgbm1 libatk-bridge2.0-0 libcups2 libxkbcommon-x11-0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgtk-3-0 libxss1 libxshmfence1```
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