# Childhood Game (Angular 19 Rewrite)

This project was rewritten from an Express + EJS server-rendered app to an **Angular 19** frontend with an Express API backend.

## Stack

- Angular 19 (standalone components)
- Express 4
- PostgreSQL (`pg`)

## Run locally

```bash
npm install
npm run build
npm start
```

The app serves Angular from `dist/childhoodgame/browser` and provides an API at:

- `GET /api/assets`

If PostgreSQL is unreachable, the API returns fallback seed data so the UI still loads.

## Development mode

```bash
npm run dev
```

This runs both Express and Angular dev server concurrently.
