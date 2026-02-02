import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { waybillService } from '../services/waybillService';
import type { WaybillDto, UpdateWaybillStatusDto } from '../types/api';
import { formatters } from '../utils/formatters';
import '../styles/components.css';

// Convert numeric status to string
const getStatusString = (status: string | number): string => {
  if (typeof status === 'number') {
    switch (status) {
      case 0: return 'PENDING';
      case 1: return 'DELIVERED';
      case 2: return 'CANCELLED';
      case 3: return 'DISPUTED';
      default: return 'UNKNOWN';
    }
  }
  return status.toUpperCase();
};

export const WaybillDetails = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [waybill, setWaybill] = useState<WaybillDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [newStatus, setNewStatus] = useState<string>('');
  const [statusNotes, setStatusNotes] = useState<string>('');
  const [updatingStatus, setUpdatingStatus] = useState(false);

  useEffect(() => {
    if (id) {
      loadWaybill();
    }
  }, [id]);

  const loadWaybill = async () => {
    if (!id) return;

    setLoading(true);
    setError(null);

    try {
      const data = await waybillService.getWaybill(id);
      setWaybill(data);
      // Handle both string and numeric status
      const statusStr = typeof data.status === 'number' 
        ? getStatusString(data.status) 
        : data.status;
      setNewStatus(statusStr);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load waybill');
    } finally {
      setLoading(false);
    }
  };

  const handleStatusUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id || !newStatus) return;

    setUpdatingStatus(true);
    setError(null);

    try {
      const dto: UpdateWaybillStatusDto = {
        status: newStatus as any,
        notes: statusNotes || undefined,
      };
      const updated = await waybillService.updateStatus(id, dto);
      setWaybill(updated);
      setStatusNotes('');
      alert('Status updated successfully');
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update status');
    } finally {
      setUpdatingStatus(false);
    }
  };

  if (loading) {
    return <div className="loading">Loading waybill...</div>;
  }

  if (error && !waybill) {
    return (
      <div className="error-message">
        <p>{error}</p>
        <button onClick={() => navigate('/waybills')} className="btn btn-secondary">
          Back to List
        </button>
      </div>
    );
  }

  if (!waybill) {
    return <div>Waybill not found</div>;
  }

  return (
    <div className="waybill-details-container">
      <div className="details-header">
        <h1>Waybill {waybill.id}</h1>
        <button onClick={() => navigate('/waybills')} className="btn btn-secondary">
          Back to List
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="details-grid">
        <div className="details-section">
          <h2>Basic Information</h2>
          <dl>
            <dt>Waybill ID</dt>
            <dd>{waybill.id}</dd>
            <dt>Waybill Date</dt>
            <dd>{formatters.formatDate(waybill.waybillDate)}</dd>
            <dt>Delivery Date</dt>
            <dd>{formatters.formatDate(waybill.deliveryDate)}</dd>
            <dt>Status</dt>
            <dd>
              <span className={`status-badge status-${getStatusString(waybill.status).toLowerCase()}`}>
                {getStatusString(waybill.status)}
              </span>
            </dd>
          </dl>
        </div>

        <div className="details-section">
          <h2>Project & Supplier</h2>
          <dl>
            <dt>Project</dt>
            <dd>{waybill.projectName} ({waybill.projectId})</dd>
            <dt>Supplier</dt>
            <dd>{waybill.supplierName} ({waybill.supplierId})</dd>
          </dl>
        </div>

        <div className="details-section">
          <h2>Product Information</h2>
          <dl>
            <dt>Product Code</dt>
            <dd>{waybill.productCode}</dd>
            <dt>Product Name</dt>
            <dd>{waybill.productName}</dd>
            <dt>Quantity</dt>
            <dd>{formatters.formatQuantity(waybill.quantity, waybill.unit)}</dd>
            <dt>Unit Price</dt>
            <dd>{formatters.formatCurrency(waybill.unitPrice, waybill.currency)}</dd>
            <dt>Total Amount</dt>
            <dd>{formatters.formatCurrency(waybill.totalAmount, waybill.currency)}</dd>
          </dl>
        </div>

        <div className="details-section">
          <h2>Delivery Information</h2>
          <dl>
            {waybill.vehicleNumber && (
              <>
                <dt>Vehicle Number</dt>
                <dd>{waybill.vehicleNumber}</dd>
              </>
            )}
            {waybill.driverName && (
              <>
                <dt>Driver Name</dt>
                <dd>{waybill.driverName}</dd>
              </>
            )}
            {waybill.deliveryAddress && (
              <>
                <dt>Delivery Address</dt>
                <dd>{waybill.deliveryAddress}</dd>
              </>
            )}
          </dl>
        </div>

        {waybill.notes && (
          <div className="details-section full-width">
            <h2>Notes</h2>
            <p className="notes-text">{waybill.notes}</p>
          </div>
        )}

        <div className="details-section full-width">
          <h2>Update Status</h2>
          <form onSubmit={handleStatusUpdate} className="status-update-form">
            <div className="form-group">
              <label htmlFor="newStatus">New Status</label>
              <select
                id="newStatus"
                value={newStatus}
                onChange={(e) => setNewStatus(e.target.value)}
                className="form-control"
                required
              >
                <option value="">Select Status</option>
                <option value="PENDING">Pending</option>
                <option value="DELIVERED">Delivered</option>
                <option value="CANCELLED">Cancelled</option>
                <option value="DISPUTED">Disputed</option>
              </select>
            </div>
            <div className="form-group">
              <label htmlFor="statusNotes">Notes (Optional)</label>
              <textarea
                id="statusNotes"
                value={statusNotes}
                onChange={(e) => setStatusNotes(e.target.value)}
                className="form-control"
                rows={3}
              />
            </div>
            <button
              type="submit"
              disabled={updatingStatus || newStatus === waybill.status}
              className="btn btn-primary"
            >
              {updatingStatus ? 'Updating...' : 'Update Status'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
