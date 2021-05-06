# Configuring nginx proxy
First you should create and enable nginx config for your website
than edit it replace **web** with your config name
```sudo nano /etc/nginx/sites-enabled/web```

Sample config below
```
server {
    listen        80;
    server_name   example.com *.example.com;
    
    location / {
        proxy_pass         http://127.0.0.1:4999;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```