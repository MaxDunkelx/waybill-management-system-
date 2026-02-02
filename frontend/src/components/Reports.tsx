import { useState } from 'react';
import { reportsService } from '../services/reportsService';
import type { MonthlyReportResult } from '../types/api';
import { LoadingSpinner } from './common/LoadingSpinner';
import { ErrorMessage } from './common/ErrorMessage';
import { formatters } from '../utils/formatters';
import '../styles/components.css';

export const Reports= () => {
  const [year, setYear] = useState(new Date().getFullYear());
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<MonthlyReportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [conflict, setConflict] = useState(false);

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      setError(null);
      setConflict(false);
      const data = await reportsService.generateMonthlyReport(year, month);
      setResult(data);
    } catch (err: any) {
      if (err.response?.status === 409) {
        setConflict(true);
        setError('Report generation is already in progress. Please try again later.');
      } else if (err.response?.status === 400) {
        setError(err.response?.data?.message || 'Invalid year or month. Please check your input.');
      } else {
        setError(err.message || 'Failed to generate report');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="reports-container">
      <h1>Monthly Reports</h1>
      <p>Generate monthly reports. Only one report can be generated at a time.</p>

      <form onSubmit={handleGenerate} className="report-form">
        <div className="form-group">
          <label htmlFor="year">Year</label>
          <input
            type="number"
            id="year"
            value={year}
            onChange={(e) => setYear(parseInt(e.target.value))}
            min="2020"
            max="2030"
            className="form-control"
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="month">Month</label>
          <select
            id="month"
            value={month}
            onChange={(e) => setMonth(parseInt(e.target.value))}
            className="form-control"
            required
          >
            {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
              <option key={m} value={m}>
                {new Date(2000, m - 1).toLocaleDateString('en-US', { month: 'long' })}
              </option>
            ))}
          </select>
        </div>

        <button
          type="submit"
          disabled={loading}
          className="btn btn-primary btn-block"
        >
          {loading ? 'Generating...' : 'Generate Report'}
        </button>
      </form>

      {loading && <LoadingSpinner />}
      {error && (
        <ErrorMessage
          message={error}
          onRetry={conflict ? undefined : () => handleGenerate(new Event('submit') as any)}
        />
      )}

      {result && (
        <div className="report-result success">
          <h3>Monthly Report - {new Date(2000, (result.month || 1) - 1).toLocaleDateString('en-US', { month: 'long' })} {result.year || 'N/A'}</h3>
          <p className="report-meta">Generated at: {result.generatedAt ? formatters.formatDateTime(result.generatedAt) : 'N/A'}</p>

          {/* Summary Cards */}
          <div className="report-summary-cards">
            <div className="summary-card">
              <h4>Total Waybills</h4>
              <div className="summary-value">{result.totalWaybills ?? 0}</div>
            </div>
            <div className="summary-card">
              <h4>Total Quantity</h4>
              <div className="summary-value">{formatters.formatQuantity(result.totalQuantity ?? 0, 'מ"ק')}</div>
            </div>
            <div className="summary-card">
              <h4>Total Amount</h4>
              <div className="summary-value">{formatters.formatCurrency(result.totalAmount ?? 0)}</div>
            </div>
          </div>

          {/* Status Breakdown */}
          {result.statusBreakdown && result.statusBreakdown.length > 0 && (
            <section className="report-section">
              <h4>Status Breakdown</h4>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Status</th>
                    <th>Count</th>
                    <th>Total Quantity</th>
                    <th>Total Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {result.statusBreakdown.map((status, idx) => (
                    <tr key={idx}>
                      <td>{status.status ?? 'Unknown'}</td>
                      <td>{status.count ?? 0}</td>
                      <td>{formatters.formatQuantity(status.totalQuantity ?? 0, 'מ"ק')}</td>
                      <td>{formatters.formatCurrency(status.totalAmount ?? 0)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          {/* Top Suppliers */}
          {result.topSuppliers && result.topSuppliers.length > 0 && (
            <section className="report-section">
              <h4>Top Suppliers</h4>
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
                  {result.topSuppliers.map((supplier) => (
                    <tr key={supplier.supplierId}>
                      <td>{supplier.supplierName ?? 'Unknown'}</td>
                      <td>{supplier.deliveryCount ?? 0}</td>
                      <td>{formatters.formatQuantity(supplier.totalQuantity ?? 0, 'מ"ק')}</td>
                      <td>{formatters.formatCurrency(supplier.totalAmount ?? 0)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          {/* Top Projects */}
          {result.topProjects && result.topProjects.length > 0 && (
            <section className="report-section">
              <h4>Top Projects</h4>
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
                  {result.topProjects.map((project) => (
                    <tr key={project.projectId}>
                      <td>{project.projectName ?? 'Unknown'}</td>
                      <td>{project.waybillCount ?? 0}</td>
                      <td>{formatters.formatQuantity(project.totalQuantity ?? 0, 'מ"ק')}</td>
                      <td>{formatters.formatCurrency(project.totalAmount ?? 0)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          {/* Product Breakdown */}
          {result.productBreakdown && result.productBreakdown.length > 0 && (
            <section className="report-section">
              <h4>Product Breakdown</h4>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Product</th>
                    <th>Count</th>
                    <th>Total Quantity</th>
                    <th>Total Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {result.productBreakdown.map((product) => (
                    <tr key={product.productCode}>
                      <td>{product.productName ?? 'Unknown'} ({product.productCode ?? 'N/A'})</td>
                      <td>{product.count ?? 0}</td>
                      <td>{formatters.formatQuantity(product.totalQuantity ?? 0, 'מ"ק')}</td>
                      <td>{formatters.formatCurrency(product.totalAmount ?? 0)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}
        </div>
      )}
    </div>
  );
};
