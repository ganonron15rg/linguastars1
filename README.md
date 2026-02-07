# LinguaStars Monorepo

## Overview
This repository contains the Unity client, FastAPI server, and React dashboards for LinguaStars V1.

## Run the Server Locally
```bash
cd server
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload
```

## Run the Dashboards
Parent dashboard:
```bash
cd dashboards/parent-web
npm install
npm run dev
```

Admin dashboard:
```bash
cd dashboards/admin-web
npm install
npm run dev
```

## Unity Client Content JSON
Unity content JSON lives under:
```
client-unity/Assets/StreamingAssets/content/
```
