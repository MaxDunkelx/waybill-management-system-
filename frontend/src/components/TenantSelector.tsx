import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/components.css';

const AVAILABLE_TENANTS = ['TENANT001', 'TENANT002', 'TENANT003'];

export const TenantSelector = () => {
  const [selectedTenant, setSelectedTenant] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!selectedTenant) {
      setError('Please select a tenant');
      return;
    }

    // Store tenant ID in localStorage
    localStorage.setItem('tenantId', selectedTenant);
    
    // Navigate to main app
    navigate('/waybills');
  };

  const handleLogout = () => {
    localStorage.removeItem('tenantId');
    setSelectedTenant('');
  };

  const currentTenant = localStorage.getItem('tenantId');

  if (currentTenant) {
    return (
      <div className="tenant-selector">
        <div className="current-tenant">
          <h2>Current Tenant</h2>
          <p className="tenant-name">{currentTenant}</p>
          <button onClick={handleLogout} className="btn btn-secondary">
            Switch Tenant
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="tenant-selector">
      <h1>Waybill Management System</h1>
      <p>Please select your tenant to continue</p>
      
      <form onSubmit={handleSubmit} className="tenant-form">
        <div className="form-group">
          <label htmlFor="tenant">Tenant ID</label>
          <select
            id="tenant"
            value={selectedTenant}
            onChange={(e) => {
              setSelectedTenant(e.target.value);
              setError(null);
            }}
            className="form-control"
            required
          >
            <option value="">-- Select Tenant --</option>
            {AVAILABLE_TENANTS.map((tenant) => (
              <option key={tenant} value={tenant}>
                {tenant}
              </option>
            ))}
          </select>
        </div>

        {error && <div className="error-message">{error}</div>}

        <button type="submit" className="btn btn-primary btn-block">
          Continue
        </button>
      </form>
    </div>
  );
};
