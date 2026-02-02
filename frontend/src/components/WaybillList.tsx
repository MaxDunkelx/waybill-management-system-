import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { waybillService } from '../services/waybillService';
import type { WaybillListDto, WaybillFilterDto, PagedResult } from '../types/api';
import { formatters } from '../utils/formatters';
import '../styles/components.css';

// Convert status (string or number) to uppercase string for display
const getStatusString = (status: string | number): string => {
  // If it's already a string, convert to uppercase
  if (typeof status === 'string') {
    return status.toUpperCase();
  }
  
  // If it's a number, convert to string
  switch (status) {
    case 0: return 'PENDING';
    case 1: return 'DELIVERED';
    case 2: return 'CANCELLED';
    case 3: return 'DISPUTED';
    default: return 'UNKNOWN';
  }
};

export const WaybillList = () => {
  const [waybills, setWaybills] = useState<WaybillListDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<WaybillFilterDto>({
    page: 1,
    pageSize: 20,
  });
  const [pagination, setPagination] = useState({
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
  });

  useEffect(() => {
    loadWaybills();
  }, [filter]);

  const loadWaybills = async () => {
    setLoading(true);
    setError(null);

    try {
      const result: PagedResult<WaybillListDto> = await waybillService.getWaybills(filter);
      setWaybills(result.items || []);
      setPagination({
        totalCount: result.totalCount || 0,
        page: result.pageNumber || 1,
        pageSize: result.pageSize || 20,
        totalPages: result.totalPages || 0,
      });
    } catch (err: any) {
      console.error('Error loading waybills:', err);
      const errorMessage = err.response?.data?.message || 
                          err.response?.data?.error || 
                          err.message || 
                          'Failed to load waybills';
      setError(`Network Error: ${errorMessage}`);
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (field: keyof WaybillFilterDto, value: any) => {
    setFilter((prev) => ({ ...prev, [field]: value, page: 1 }));
  };

  const handlePageChange = (newPage: number) => {
    setFilter((prev) => ({ ...prev, page: newPage }));
  };

  return (
    <div className="waybill-list-container">
      <h1>Waybills</h1>

      <div className="filters-section">
        <div className="filter-row">
          <div className="form-group">
            <label>Status</label>
            <select
              value={filter.status || ''}
              onChange={(e) => handleFilterChange('status', e.target.value || undefined)}
              className="form-control"
            >
              <option value="">All</option>
              <option value="PENDING">Pending</option>
              <option value="DELIVERED">Delivered</option>
              <option value="CANCELLED">Cancelled</option>
              <option value="DISPUTED">Disputed</option>
            </select>
          </div>

          <div className="form-group">
            <label>Search</label>
            <input
              type="text"
              value={filter.searchText || ''}
              onChange={(e) => handleFilterChange('searchText', e.target.value || undefined)}
              placeholder="Search project, supplier, product..."
              className="form-control"
            />
          </div>

          <div className="form-group">
            <label>From Date</label>
            <input
              type="date"
              value={filter.waybillDateFrom || ''}
              onChange={(e) => handleFilterChange('waybillDateFrom', e.target.value || undefined)}
              className="form-control"
            />
          </div>

          <div className="form-group">
            <label>To Date</label>
            <input
              type="date"
              value={filter.waybillDateTo || ''}
              onChange={(e) => handleFilterChange('waybillDateTo', e.target.value || undefined)}
              className="form-control"
            />
          </div>
        </div>
      </div>

      {error && (
        <div className="error-message">
          <strong>Error:</strong> {error}
          <br />
          <small>Check browser console (F12) for details</small>
        </div>
      )}

      {loading ? (
        <div className="loading">Loading waybills...</div>
      ) : waybills.length === 0 && !error ? (
        <div className="empty-state">
          <p>No waybills found. Import a CSV file to get started.</p>
          <p><a href="/import" className="btn btn-primary">Go to Import</a></p>
        </div>
      ) : (
        <>
          <div className="waybills-table-container">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Waybill ID</th>
                  <th>Date</th>
                  <th>Project</th>
                  <th>Supplier</th>
                  <th>Product</th>
                  <th>Amount</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {waybills.map((waybill) => {
                  const statusString = getStatusString(waybill.status);
                  return (
                    <tr key={waybill.id}>
                      <td>{waybill.id}</td>
                      <td>{formatters.formatDate(waybill.waybillDate)}</td>
                      <td>{waybill.projectName}</td>
                      <td>{waybill.supplierName}</td>
                      <td>{waybill.productName} ({waybill.productCode})</td>
                      <td>{formatters.formatCurrency(waybill.totalAmount, waybill.currency)}</td>
                      <td>
                        <span className={`status-badge status-${statusString.toLowerCase()}`}>
                          {statusString}
                        </span>
                      </td>
                      <td>
                        <Link to={`/waybills/${waybill.id}`} className="btn btn-sm btn-primary">
                          View
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {pagination.totalPages > 1 && (
            <div className="pagination">
              <button
                onClick={() => handlePageChange(pagination.page - 1)}
                disabled={pagination.page <= 1}
                className="btn btn-secondary"
              >
                Previous
              </button>
              <span>
                Page {pagination.page} of {pagination.totalPages} ({pagination.totalCount} total)
              </span>
              <button
                onClick={() => handlePageChange(pagination.page + 1)}
                disabled={pagination.page >= pagination.totalPages}
                className="btn btn-secondary"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};
