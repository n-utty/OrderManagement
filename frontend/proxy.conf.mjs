// Dev-server proxy: /api goes to the backend, so the app and API share an origin (no CORS).
// Defaults to the API's "http" launch profile; set API_URL when running another profile,
// e.g. API_URL=https://localhost:44323 for IIS Express.
const target = process.env.API_URL || 'http://localhost:5224';

export default {
  '/api': {
    target,
    // Accepts the self-signed development certificate when the target is https.
    secure: false,
    changeOrigin: true,
  },
};
