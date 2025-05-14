# Documentation for running Insurance Bot

## Description

Insurance Bot is a telegram bot that helps users get insurance.
Automates the process of collecting data, processing documents and gradual insurance in text format.
The main goal of the bot is to simplify the process of getting insurance and make it available via Telegram.

## Requirements

* Docker installed on your device.
* Telegram account.

## Installation and launch

1. Open a terminal and go to the project folder:

```bash
  cd /path/to/folder/with/project
```

2. Build a Docker image named `insurance-bot`:

```bash
  docker build -t insurance-bot .
```

3. Run the container:

```bash
  docker run -d -p 8080:80 --env-file .env --name InsuranceBot insurance_bot
```

## Usage

After the container has successfully launched, go to Telegram and find the bot by name:
[@insurance\_auto\_diceus\_bot](https://t.me/insurance_auto_diceus_bot)

The bot is now ready to use!

---

## Bot workflow

### Getting started

Interaction with the bot begins with a welcome message.
The user clicks the **START** button, after which the insurance process begins.
The bot immediately requests a photo of the ID card.

### Processing documents

After successfully uploading the ID card photo, the bot sends a request to extract data.
If the data is successfully received, the bot moves to the next step.

At the next stage, the bot asks to upload the front side of the vehicle registration certificate.
After successful processing, the bot asks to upload the back side of the registration certificate.

### Confirming data

The bot displays all the collected data and asks the user to confirm its correctness.
If the data is correct, the process continues.
If the user does not agree with the data provided, the bot restarts the process
and again requests to upload all the photos in the same order.

### Confirmation of insurance registration

After confirming the data, the bot notifies the user about the cost of insurance: **100\$**.
The user can either agree to the registration or refuse.

If the user agrees, the bot generates a text version of the insurance and sends it to the chat.
If the user refuses, the registration process is reset.

---

The bot uses AI to generate messages.