# Telegram Chat Archiver

A background service for automatically archiving messages from a specified Telegram chat to local Markdown files.

## Features

-   Connects to the Telegram API using a bot token.
-   Fetches new messages from a specified chat.
-   Processes text, voice messages, images, and files.
-   Transcribes voice messages locally using Whisper.net.
-   Saves messages to daily Markdown files.
-   Saves attachments to a separate directory and links to them in the Markdown file.
-   Sends error notifications to the Telegram chat.

## Configuration

The service is configured using environment variables. You can create a `.env` file in the root of the project and add the following variables:

```
TELEGRAM_BOT_TOKEN=YOUR_BOT_TOKEN
TELEGRAM_CHAT_ID=YOUR_CHAT_ID
WHISPER_MODEL_NAME=ggml-base.bin
STORAGE_WORKING_DIRECTORY=/app/data
STORAGE_ATTACHMENTS_FOLDER=attachments
FORMATTING_FILE_NAME_MASK="Telegram-yyyy-MM-dd'_Notes.md'"
FORMATTING_HEADER_MASK="'#### [['yyyy-MM-dd ddd']]' HH:mm:ss"
```

### Whisper Model

The `WHISPER_MODEL_NAME` variable specifies the name of the Whisper model file to use for transcription. The `Dockerfile` downloads the `ggml-base.bin` model by default. If you want to use a different model, you will need to modify the `Dockerfile` to download the desired model.

## Deployment

The service is deployed using Docker.

1.  Build the Docker image:

    ```
    docker build -t your-docker-hub-username/telegram-chat-archiver:latest .
    ```

2.  Create a `.env` file with your configuration.

3.  Run the Docker container:

    ```
    docker run -d --env-file .env -v ./data:/app/data your-docker-hub-username/telegram-chat-archiver:latest
    ```

You can also use the provided `docker-compose.yml` file to run the service:

```
docker-compose up -d
```
