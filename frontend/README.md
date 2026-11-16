# Frontend

The Angular app for Order Intake & Tracking: the order list, detail and create pages. In development it calls the API through a dev-server proxy (`/api`).

```bash
npm install
npm start                  # http://localhost:4200
npm test -- --watch=false
```

The backend must be running for `npm start`. See the [root README](../README.md) for full setup, including the `API_URL` setting that points the proxy at the right backend launch profile.
