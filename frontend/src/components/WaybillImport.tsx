import { useState } from 'react';
import { importService } from '../services/importService';
import type { ImportResult } from '../types/api';
import '../styles/components.css';

export const WaybillImport = () => {
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
      setResult(null);
      setError(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!file) {
      setError('Please select a file');
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const importResult = await importService.importWaybills(file);
      setResult(importResult);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Import failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="import-container">
      <h1>Import Waybills</h1>
      <p>Upload a CSV file containing waybill data. The file must be UTF-8 encoded.</p>

      <form onSubmit={handleSubmit} className="import-form">
        <div className="form-group">
          <label htmlFor="file">CSV File</label>
          <input
            type="file"
            id="file"
            accept=".csv"
            onChange={handleFileChange}
            className="form-control"
            required
          />
        </div>

        <button
          type="submit"
          disabled={loading || !file}
          className="btn btn-primary btn-block"
        >
          {loading ? 'Importing...' : 'Import Waybills'}
        </button>
      </form>

      {error && (
        <div className="error-message">
          <strong>Error:</strong> {error}
        </div>
      )}

      {result && (
        <div className="import-result">
          <h2>Import Results</h2>
          
          <div className="result-summary">
            <div className="summary-card">
              <h3>Total Rows</h3>
              <div className="summary-value">{result.totalRows}</div>
            </div>
            <div className="summary-card success">
              <h3>Successful</h3>
              <div className="summary-value">{result.successCount}</div>
            </div>
            <div className="summary-card error">
              <h3>Errors</h3>
              <div className="summary-value">{result.errorCount}</div>
            </div>
          </div>

          {result.warnings && result.warnings.length > 0 && (
            <div className="warnings-section">
              <h3>Warnings</h3>
              <ul>
                {result.warnings.map((warning, idx) => (
                  <li key={idx}>{warning}</li>
                ))}
              </ul>
            </div>
          )}

          {result.errors && result.errors.length > 0 && (
            <div className="errors-section">
              <h3>Errors</h3>
              <div className="errors-list">
                {result.errors.map((err, idx) => (
                  <div key={idx} className="error-item">
                    <strong>Row {err.rowNumber}</strong>
                    {err.field && <span className="error-field">Field: {err.field}</span>}
                    <div className="error-message">{err.message}</div>
                    {err.rowData && (
                      <div className="error-row-data">
                        <small>Row data: {err.rowData}</small>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
