# Garmin Activity Exporter

Web app that imports activity files downloaded from Garmin Connect, parses them, and exports the results as structured plain text, ready to copy and paste to your coach.

> Unofficial project, not affiliated with or endorsed by Garmin.

## Why

This project started from a personal need. I train with a coach, and every week I had to send him my results by copying numbers by hand from Garmin Connect. It was slow and error-prone.

So I built a tool that reads the activity files exported from Garmin Connect (no official API, no OAuth, no rate limits, and my data stays local) and turns them into a compact, structured text summary I can paste straight into a message to my coach, in a format I designed myself and that it is useful for me and my coach.

## Tech stack

- **Backend:** C# / ASP.NET Core
- **Frontend:** Vue 3 + TypeScript
- **Parsing:** Garmin activity files (`.fit`)

## Project structure

```
backend/    ASP.NET Core solution (API, core logic, tests)
frontend/   Vue app
```

## Getting started

### Backend

```bash
cd backend
dotnet run --project ActivityExporter.Api
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Status

Work in progress.

## License

MIT
