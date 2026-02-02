import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import '../styles/components.css';

export const Layout = () => {
  const navigate = useNavigate();
  const tenantId = localStorage.getItem('tenantId');

  useEffect(() => {
    if (!tenantId) {
      navigate('/tenant-select');
    }
  }, [tenantId, navigate]);

  if (!tenantId) {
    return null;
  }

  const handleLogout = () => {
    localStorage.removeItem('tenantId');
    navigate('/tenant-select');
  };

  return (
    <div className="app-layout">
      <header className="app-header">
        <h1>Waybill Management System</h1>
        <div className="header-actions">
          <span className="tenant-badge">Tenant: {tenantId}</span>
          <button onClick={handleLogout} className="btn btn-secondary btn-sm">
            Switch Tenant
          </button>
        </div>
      </header>

      <nav className="app-nav">
        <Link to="/waybills">Waybills</Link>
        <Link to="/import">Import</Link>
        <Link to="/summary">Summary</Link>
        <Link to="/reports">Reports</Link>
      </nav>

      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
};
