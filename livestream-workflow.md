# Thrive Stream Controller – Livestream Workflow

## Purpose

This document describes the **supported, ToS-compliant** livestream workflow for Thrive, using:

- **OBS** as the streaming engine
- **YouTube Live** via official API
- **Facebook Live** via **persistent stream keys** and a **single manual "Go Live" click**

It replaces the previous approach that automated the Facebook UI (e.g., Playwright + saved cookies), which was fragile and likely violated Meta's terms of service.

The goal is to make Sundays **volunteer-friendly**: they follow a short checklist and click a few buttons, without needing to know about stream keys, RTMP URLs, or automation internals.

---

## High-Level Summary (Volunteer View)

On a normal Sunday, volunteers will:

1. Open **Thrive Stream Controller**.
2. Click **"Start Stream (OBS + YouTube)"**.
3. Click **"Open Facebook to Go Live"**, then in the browser:
   - Confirm the preview is receiving video.
   - Click **"Go Live"** on Facebook.
4. Switch back to Thrive/OBS to manage scenes and monitor the stream.
5. At the end of the service, click **"End Stream"** in Thrive.

Volunteers **do not**:

- Touch stream keys.
- Change OBS output settings.
- Script or automate Facebook in any way.

---

## Roles

### Technical Admin (Wyatt / Tech Lead)

Responsible for:

- One-time and occasional configuration.
- Keeping OBS, YouTube, and Facebook settings aligned.
- Maintaining OAuth credentials and app configuration.
- Updating this workflow when platforms change behavior.

### Sunday Volunteers

Responsible for:

- Following the checklist in this document.
- Using the buttons in the Thrive app and OBS scenes.
- Not changing "advanced" settings unless instructed.

---

## One-Time / Occasional Setup (Admin Only)

### 1. YouTube Live

- Set up the YouTube channel for live streaming.
- Decide whether to:
  - Use a **persistent stream key** and treat YT as a simple RTMP target, **or**
  - Use the **YouTube Live Streaming API** for scheduled events and better metadata.
- Configure the Thrive Stream Controller to:
  - Authenticate with YouTube via OAuth.
  - Either create/bind broadcasts and start them, or assume OBS sends RTMP to a persistent key.

Volunteers do **not** change YouTube's stream key.

### 2. Facebook Live (Persistent Key)

On the Facebook Page (done once, then rarely):

- Enable **Persistent Stream Key**.
- Configure the Page so that when it receives RTMP:
  - It either automatically goes live, **or**
  - It shows the incoming preview, and an operator must click **"Go Live"**.
- Record the **Facebook Live Producer URL** for the Page (this is what volunteers will open when they click "Open Facebook to Go Live" in Thrive).

Volunteers do **not** edit persistent keys.

### 3. OBS

- Install and configure **OBS WebSocket** (if not already part of OBS).
- Create a dedicated **profile** and/or **scene collection** for Sunday services.
- In that profile:
  - Set the **output/streaming service** to use the YouTube and Facebook persistent keys.
  - Confirm that when you click **"Start Streaming"**, both YT and FB receive RTMP.

Volunteers should only:

- Use the correct profile/scene collection.
- Switch scenes as appropriate.
- Never touch the stream key fields.

### 4. Thrive Stream Controller App

- Configure the app to:
  - Connect to OBS WebSocket (host/port/password).
  - Use YouTube OAuth credentials.
  - Store the Facebook Live Producer URL.
- Implement three key actions:
  - **Start Stream (OBS + YouTube)**
  - **Open Facebook to Go Live**
  - **End Stream**

---

## Weekly Sunday Workflow (Volunteers)

### 1. Pre-Service Checks

Before people arrive:

1. **Open OBS**
   - Ensure the correct **profile** and **scene collection** are selected.
   - Confirm camera, audio, and scenes look correct.

2. **Open Thrive Stream Controller**
   - Ensure it shows that OBS is **connected**.
   - Ensure it shows YouTube and Facebook statuses (if available).

3. **Do NOT** open Facebook or YouTube for configuration.
   - Only open them later when instructed to **"Go Live"** on Facebook.

### 2. Starting the Stream (OBS + YouTube)

From Thrive Stream Controller:

1. Click **"Start Stream (OBS + YouTube)"**.
2. The app will:
   - Connect to OBS and trigger **Start Streaming**.
   - Start or transition the YouTube broadcast to **live** (if using API mode).
3. Watch the status indicators:
   - **OBS**: should show "Streaming".
   - **YouTube**: should show "Live" or similar status.
4. If the app indicates an error (e.g., OBS not connected), follow the troubleshooting section below.

At this point, OBS is sending RTMP to both platforms using the preconfigured persistent keys.

### 3. Going Live on Facebook (Minimal Manual Step)

After **Start Stream** succeeds:

1. In Thrive, click **"Open Facebook to Go Live"**.
2. A browser tab will open to the **Facebook Live Producer** page for our Page.
3. On that page:
   - Verify that a **preview video** is present (this confirms OBS RTMP is coming through).
   - Confirm title/description if needed (should be pre-configured by the admin).
   - Click **"Go Live"**.
4. Once live:
   - You may close the browser tab if you don't need to see comments or other FB controls.
   - Return to Thrive/OBS to manage scenes.

Key point: this is the **only** Facebook interaction expected from volunteers.

### 4. During the Service

Use OBS and/or Thrive for monitoring:

- In **OBS**:
  - Watch audio levels.
  - Switch between prepared scenes.
- In **Thrive Stream Controller**:
  - Confirm OBS is streaming.
  - Confirm YouTube is live.
  - Optionally, confirm Facebook status (if the app polls the Graph API read-only).

If any status indicator turns red or shows "disconnected", refer to troubleshooting.

### 5. Ending the Stream

When the service is over:

1. In Thrive Stream Controller, click **"End Stream"**.
2. The app will:
   - Tell OBS to **Stop Streaming**.
   - End/complete the YouTube broadcast (if using the API mode).
3. Facebook will automatically end the live when RTMP stops.

You do **not** need to manually end the live on YouTube or Facebook unless something unusual happens.

---

## Why We Stopped Automating Facebook with Playwright

Previously, the system attempted to:

- Save Facebook login cookies into a JSON file.
- Use **Playwright** (or similar) to:
  - Load the cookies.
  - Log in to Facebook as a user.
  - Navigate to the Producer UI.
  - Click the **"Go Live"** button automatically.

This approach had several critical issues:

### Platform Enforcement

- Facebook/Meta explicitly tries to detect automated logins and scripted UI actions.
- This leads to:
  - CAPTCHAs.
  - 2-factor authentication prompts.
  - "Suspicious login" security checks.

### Reliability

- Even small UI or HTML changes can break the automation.
- It's not acceptable to have the stream fail because a selector or button text changed.

### Compliance

- Automating a consumer UI via headless browser and saved cookies generally conflicts with platform **terms of service**.
- We want to avoid any practice that could get the Page or accounts flagged, limited, or blocked.

For these reasons, the official policy is now:

- **We do not automate Facebook's UI.**
- We only:
  - Use Facebook's APIs in a compliant, read-only way (if at all).
  - Use persistent stream keys.
  - Have a human operator perform the single **"Go Live"** click in the browser.

This keeps the system stable, understandable, and within Meta's rules.

---

## Troubleshooting Quick Reference

### OBS Shows "Not Connected" in Thrive

- Make sure OBS is running.
- Confirm the OBS WebSocket plugin is enabled and using the expected port/password.
- Close and reopen Thrive Stream Controller if needed.

### YouTube is Not Live After Clicking "Start Stream"

- Check that internet connectivity is stable.
- If Thrive provides an error message, read it and follow guidance.
- As a fallback, open YouTube Studio:
  - Confirm that a live stream with the correct key is receiving video.
  - If OBS is streaming but YT is not live, contact the admin.

### Facebook Preview Shows No Video

- Confirm OBS is actually streaming (status in OBS and Thrive).
- Double-check that the correct OBS profile with FB's persistent key is active.
- If no video appears after a short wait:
  - Stop and restart streaming from Thrive.
  - If the issue persists, contact the admin and do **not** try to change keys.

### Forgot to Click "Go Live" on Facebook

- If the service is already in progress:
  - Click **"Open Facebook to Go Live"** and then "Go Live" in Facebook as soon as possible.
  - The recording on YouTube will be complete; Facebook will start from the moment you finally go live.

---

## Summary

- **Volunteers**:
  - Use Thrive Stream Controller and OBS.
  - Click **Start Stream**, **Open Facebook to Go Live**, and **End Stream**.
  - Do not touch keys or advanced settings.

- **Admins**:
  - Maintain persistent keys, OBS profiles, and app configuration.
  - Keep this workflow aligned with changes in YouTube and Facebook behavior.

This gets us as close as practical to a **single-button livestream controller** while remaining **stable and compliant** with platform rules.

