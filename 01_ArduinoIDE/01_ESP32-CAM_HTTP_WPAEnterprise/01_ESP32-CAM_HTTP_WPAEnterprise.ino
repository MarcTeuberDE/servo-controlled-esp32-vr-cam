#include "esp_camera.h"
#include <esp_wifi.h>
#include "esp_wpa2.h"
#include <WiFi.h>
#include <WebServer.h>


// CAMERA MODEL
#define CAMERA_MODEL_AI_THINKER
#include "camera_pins.h"

// WiFi credentials
#include "wifi_credentials.h" //wifi_credentials.h consists of Burgnetz SSID, and my personal username and password
                              //   - in your case you would need to create  your own wifi_credentials.h file with your wifi creds

WebServer server(80);

void connectToEnterpriseWiFi() {
  WiFi.disconnect(true);
  delay(1000);

  WiFi.mode(WIFI_STA);
  esp_wifi_sta_wpa2_ent_set_identity((uint8_t *)username, strlen(username));
  esp_wifi_sta_wpa2_ent_set_username((uint8_t *)username, strlen(username));
  esp_wifi_sta_wpa2_ent_set_password((uint8_t *)password, strlen(password));

  esp_wifi_sta_wpa2_ent_enable();
  WiFi.begin(ssid);

  Serial.println("Connecting to Enterprise WiFi...");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("");
  Serial.println("WiFi connected");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
}

void streamHandler() {
  WiFiClient client = server.client();

  String response = "HTTP/1.1 200 OK\r\n";
  response += "Content-Type: multipart/x-mixed-replace; boundary=frame\r\n\r\n";
  server.sendContent(response);

  while (1) {
    camera_fb_t *fb = esp_camera_fb_get();
    if (!fb) {
      Serial.println("Camera capture failed");
      return;
    }

    response = "--frame\r\n";
    response += "Content-Type: image/jpeg\r\n\r\n";
    server.sendContent(response);

    server.sendContent((const char *)fb->buf, fb->len);
    server.sendContent("\r\n");

    esp_camera_fb_return(fb);

    if (!client.connected()) {
      break;
    }
  }
}

void startCameraServer() {
  server.on("/", HTTP_GET, streamHandler);
  server.begin();
}

void setup() {
  Serial.begin(115200);
  Serial.setDebugOutput(false);
  Serial.println();

  camera_config_t config;
  config.ledc_channel = LEDC_CHANNEL_0;
  config.ledc_timer = LEDC_TIMER_0;
  config.pin_d0 = Y2_GPIO_NUM;
  config.pin_d1 = Y3_GPIO_NUM;
  config.pin_d2 = Y4_GPIO_NUM;
  config.pin_d3 = Y5_GPIO_NUM;
  config.pin_d4 = Y6_GPIO_NUM;
  config.pin_d5 = Y7_GPIO_NUM;
  config.pin_d6 = Y8_GPIO_NUM;
  config.pin_d7 = Y9_GPIO_NUM;
  config.pin_xclk = XCLK_GPIO_NUM;
  config.pin_pclk = PCLK_GPIO_NUM;
  config.pin_vsync = VSYNC_GPIO_NUM;
  config.pin_href = HREF_GPIO_NUM;
  config.pin_sccb_sda = SIOD_GPIO_NUM;
  config.pin_sccb_scl = SIOC_GPIO_NUM;
  config.pin_pwdn = PWDN_GPIO_NUM;
  config.pin_reset = RESET_GPIO_NUM;
  config.xclk_freq_hz = 20000000;
  config.frame_size = FRAMESIZE_QVGA;
  config.pixel_format = PIXFORMAT_JPEG;
  config.grab_mode = CAMERA_GRAB_WHEN_EMPTY;
  config.fb_location = CAMERA_FB_IN_PSRAM;
  config.jpeg_quality = 10;
  config.fb_count = 2;

  if (psramFound()) {
    Serial.println("PSRAM found and enabled.");
  } else {
    Serial.println("No PSRAM found.");
    config.frame_size = FRAMESIZE_SVGA;
    config.fb_location = CAMERA_FB_IN_DRAM;
  }

  esp_err_t err = esp_camera_init(&config);
  if (err != ESP_OK) {
    Serial.printf("Camera init failed with error 0x%x", err);
    return;
  }

  connectToEnterpriseWiFi();
  WiFi.setSleep(false);

  startCameraServer();

  Serial.println("Camera Ready! Stream at:");
  Serial.print("http://");
  Serial.println(WiFi.localIP());
}

void loop() {
  server.handleClient();
}
