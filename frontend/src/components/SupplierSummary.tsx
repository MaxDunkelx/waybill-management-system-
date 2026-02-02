import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { supplierService } from '../services/supplierService';
import type { SupplierSummaryDto } from '../types/api';
import { formatters } from '../utils/formatters';
import '../styles/components.css';

export const SupplierSummary = () => {
  const { id } = useParams<{ id: string }>();
  const [summary, setSummary] = useState<SupplierSummaryDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadSummary();
    }
  }, [id]);

  const loadSummary = async () => {
    if (!id) return;

    setLoading(true);
    setError(null);

    try {
      const data = await supplierService.getSupplierSummary(id);
      setSummary(data);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load supplier summary');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Loading supplier summary...</div>;
  }

  if (error) {
    return <div className="error-message">{error}</div>;
  }

  if (!summary) {
    return <div>Supplier summary not found</div>;
  }

  return (
    <div className="supplier-summary-container">
      <h1>Supplier Summary: {summary.supplierName}</h1>

      <div className="summary-cards">
        <div className="summary-card">
          <h3>Total Deliveries</h3>
          <div className="summary-value">{summary.totalDeliveries}</div>
        </div>
        <div className="summary-card">
          <h3>Total Quantity</h3>
          <div className="summary-value">{formatters.formatQuantity(summary.totalQuantity, 'מ"ק')}</div>
        </div>
        <div className="summary-card">
          <h3>Total Amount</h3>
          <div className="summary-value">{formatters.formatCurrency(summary.totalAmount)}</div>
        </div>
        <div className="summary-card">
          <h3>Average per Delivery</h3>
          <div className="summary-value">
            {formatters.formatQuantity(summary.averageQuantityPerDelivery, 'מ"ק')}
          </div>
        </div>
      </div>

      {summary.statusBreakdown && Object.keys(summary.statusBreakdown).length > 0 && (
        <div className="summary-section">
          <h2>Status Breakdown</h2>
          <table className="data-table">
            <thead>
              <tr>
                <th>Status</th>
                <th>Count</th>
              </tr>
            </thead>
            <tbody>
              {Object.entries(summary.statusBreakdown).map(([status, count]) => (
                <tr key={status}>
                  <td>{status}</td>
                  <td>{count}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
