# Gekko Waybill Management - Frontend

React frontend application for the Waybill Management System.

## Features

- **Multi-Tenant Support**: Secure tenant selection and isolation
- **Complete API Integration**: All backend endpoints integrated
- **Modern UI**: Clean, responsive design with plain CSS
- **Hebrew Text Support**: Proper display of Hebrew text
- **Real-time Updates**: Loading states and error handling
- **Security**: Tenant ID stored securely, never exposed in URLs

## Technology Stack

- React 18
- TypeScript
- Vite (build tool)
- React Router (navigation)
- Axios (HTTP client)
- Plain CSS (no UI library dependencies)

## Setup

1. **Install dependencies**:
   ```bash
   npm install
   ```

2. **Start development server**:
   ```bash
   npm run dev
   ```

3. **Access the application**:
   - Open http://localhost:3000 in your browser
   - The Vite dev server proxies API requests to the backend

## Building for Production

```bash
npm run build
```

The built files will be in the `dist/` directory.

## Security: Tenant ID Handling

The frontend implements secure tenant ID management:

- **Storage**: Tenant ID stored in `localStorage` (persists across sessions)
- **Never in URLs**: Tenant ID never appears in URLs or query parameters
- **Header Only**: Tenant ID only sent in `X-Tenant-ID` HTTP header
- **Not Displayed**: Tenant ID not shown in UI (only "Company 1", "Company 2", etc.)
- **Automatic**: All API requests automatically include the tenant header

## Project Structure

```
frontend/
├── src/
│   ├── components/        # React components
│   │   ├── common/        # Reusable components (Header, Loading, Error)
│   │   └── [Pages]        # Page components
│   ├── services/          # API service functions
│   ├── context/           # React Context (TenantContext)
│   ├── types/             # TypeScript type definitions
│   ├── utils/             # Utility functions
│   └── styles/            # CSS files
├── public/                # Static assets
└── package.json
```

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## Usage

1. **Login**: Select a tenant from the dropdown (TENANT001, TENANT002, TENANT003)
2. **Dashboard**: View summary statistics and overview
3. **Waybills**: Browse, filter, and search waybills
4. **Import**: Upload CSV files to import waybills
5. **Reports**: Generate monthly reports
6. **Details**: View and edit individual waybills

## API Integration

All API endpoints are integrated:

- `GET /api/v1/Waybills` - List waybills with filters
- `GET /api/v1/Waybills/{id}` - Get waybill details
- `GET /api/v1/Waybills/summary` - Get summary statistics
- `POST /api/v1/Waybills/import` - Import CSV
- `PATCH /api/v1/Waybills/{id}/status` - Update status
- `PUT /api/v1/Waybills/{id}` - Update waybill (with optimistic locking)
- `GET /api/v1/Projects/{id}/waybills` - Get project waybills
- `GET /api/v1/Suppliers/{id}/summary` - Get supplier summary
- `POST /api/v1/Reports/monthly` - Generate monthly report
- `GET /api/v1/Jobs/{jobId}` - Get job status

## Development Notes

- The frontend uses Vite's proxy configuration to forward API requests
- SSL certificate validation is disabled for local development
- All API calls automatically include the `X-Tenant-ID` header
- Error handling is implemented at the service layer
- Loading states are shown during API calls
