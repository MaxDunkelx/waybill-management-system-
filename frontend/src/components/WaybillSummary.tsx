import { useState, useEffect } from 'react';
import { waybillService } from '../services/waybillService';
import type { WaybillSummaryDto } from '../types/api';
import { formatters } from '../utils/formatters';
import '../styles/components.css';

export const WaybillSummary = () => {
  const [summary, setSummary] = useState<WaybillSummaryDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dateFrom, setDateFrom] = useState<string>('');
  const [dateTo, setDateTo] = useState<string>('');

  useEffect(() => {
    loadSummary();
  }, [dateFrom, dateTo]);

  const loadSummary = async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await waybillService.getSummary(
        dateFrom || undefined,
        dateTo || undefined
      );
      setSummary(data);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load summary');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Loading summary...</div>;
  }

  if (error) {
    return <div className="error-message">{error}</div>;
  }

  if (!summary) {
    return <div>No summary data available</div>;
  }

  return (
    <div className="summary-container">
      <h1>Waybill Summary</h1>

      <div className="filters-section">
        <div className="filter-row">
          <div className="form-group">
            <label>From Date</label>
            <input
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="form-control"
            />
          </div>
          <div className="form-group">
            <label>To Date</label>
            <input
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="form-control"
            />
          </div>
          <button onClick={loadSummary} className="btn btn-primary">
            Refresh
          </button>
        </div>
      </div>

      <div className="summary-cards">
        <div className="summary-card">
          <h3>Total by Status - Quantity</h3>
          <dl>
            {Object.entries(summary.totalQuantityByStatus || {}).map(([status, quantity]) => (
              <div key={status}>
                <dt>{status}</dt>
                <dd>{formatters.formatQuantity(quantity, 'מ"ק')}</dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="summary-card">
          <h3>Total by Status - Amount</h3>
          <dl>
            {Object.entries(summary.totalAmountByStatus || {}).map(([status, amount]) => (
              <div key={status}>
                <dt>{status}</dt>
                <dd>{formatters.formatCurrency(amount)}</dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="summary-card">
          <h3>Quality Metrics</h3>
          <dl>
            <dt>Disputed</dt>
            <dd>{summary.disputedCount} ({summary.disputedPercentage.toFixed(2)}%)</dd>
            <dt>Cancelled</dt>
            <dd>{summary.cancelledCount} ({summary.cancelledPercentage.toFixed(2)}%)</dd>
          </dl>
        </div>
      </div>

      {summary.monthlyBreakdown && summary.monthlyBreakdown.length > 0 && (
        <div className="summary-section">
          <h2>Monthly Breakdown</h2>
          <table className="data-table">
            <thead>
              <tr>
                <th>Year</th>
                <th>Month</th>
                <th>Deliveries</th>
                <th>Total Quantity</th>
                <th>Total Amount</th>
              </tr>
            </thead>
            <tbody>
              {summary.monthlyBreakdown.map((month, idx) => (
                <tr key={idx}>
                  <td>{month.year}</td>
                  <td>{month.month}</td>
                  <td>{month.deliveryCount}</td>
                  <td>{formatters.formatQuantity(month.totalQuantity, 'מ"ק')}</td>
                  <td>{formatters.formatCurrency(month.totalAmount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {summary.topSuppliers && summary.topSuppliers.length > 0 && (
        <div className="summary-section">
          <h2>Top Suppliers</h2>
          <table className="data-table">
            <thead>
              <tr>
                <th>Supplier</th>
                <th>Deliveries</th>
                <th>Total Quantity</th>
                <th>Total Amount</th>
              </tr>
            </thead>
            <tbody>
              {summary.topSuppliers.map((supplier) => (
                <tr key={supplier.supplierId}>
                  <td>{supplier.supplierName}</td>
                  <td>{supplier.deliveryCount}</td>
                  <td>{formatters.formatQuantity(supplier.totalQuantity, 'מ"ק')}</td>
                  <td>{formatters.formatCurrency(supplier.totalAmount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {summary.projectTotals && summary.projectTotals.length > 0 && (
        <div className="summary-section">
          <h2>Project Totals</h2>
          <table className="data-table">
            <thead>
              <tr>
                <th>Project</th>
                <th>Waybills</th>
                <th>Total Quantity</th>
                <th>Total Amount</th>
              </tr>
            </thead>
            <tbody>
              {summary.projectTotals.map((project) => (
                <tr key={project.projectId}>
                  <td>{project.projectName}</td>
                  <td>{project.waybillCount}</td>
                  <td>{formatters.formatQuantity(project.totalQuantity, 'מ"ק')}</td>
                  <td>{formatters.formatCurrency(project.totalAmount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
