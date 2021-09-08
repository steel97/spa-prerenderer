# Configuring systemd
Edit systemd config with command below

```sudo nano /etc/systemd/system/prerenderer.service```


# Sample configuration
Replace **ubuntu** with your username
Replace **/home/ubuntu/spa-prerenderer/** with actual path

```
[Unit]
Description=SPA-Prerenderer service

[Service]
User=ubuntu
KillMode=process
WorkingDirectory=/home/ubuntu/spa-prerenderer/
ExecStart=/home/ubuntu/spa-prerenderer/spa-prerenderer
Restart=always
TimeoutSec=30
RestartSec=5

[Install]
WantedBy=multi-user.target
```

Change permissions

```sudo chmod 664 /etc/systemd/system/prerenderer.service```


Reload daemon and enable service

```sudo systemctl daemon-reload```
```sudo systemctl enable prerenderer.service```


Start service

```sudo systemctl start prerenderer.service```


Check service status

```sudo systemctl status prerenderer.service```