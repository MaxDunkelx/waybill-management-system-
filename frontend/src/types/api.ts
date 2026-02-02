// API Response Types

export const WaybillStatus = {
  PENDING: 'PENDING',
  DELIVERED: 'DELIVERED',
  CANCELLED: 'CANCELLED',
  DISPUTED: 'DISPUTED'
} as const;

export type WaybillStatus = typeof WaybillStatus[keyof typeof WaybillStatus];

export const SyncStatus = {
  PENDING_SYNC: 'PENDING_SYNC',
  SYNCED: 'SYNCED',
  SYNC_FAILED: 'SYNC_FAILED'
} as const;

export type SyncStatus = typeof SyncStatus[keyof typeof SyncStatus];

// WaybillListDto - for list views (fewer fields)
export interface WaybillListDto {
  id: string;
  waybillDate: string;
  deliveryDate: string;
  projectName: string;
  supplierName: string;
  productCode: string;
  productName: string;
  totalAmount: number;
  currency: string;
  status: WaybillStatus | number; // API returns enum as string (e.g., "Disputed", "Delivered") or number (0=Pending, 1=Delivered, 2=Cancelled, 3=Disputed)
}

// WaybillDto - for detailed views (all fields)
export interface WaybillDto {
  id: string;
  waybillDate: string;
  deliveryDate: string;
  projectId: string;
  projectName: string;
  supplierId: string;
  supplierName: string;
  productCode: string;
  productName: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  totalAmount: number;
  currency: string;
  status: WaybillStatus | number; // Can be string enum or number (0=Pending, 1=Delivered, 2=Cancelled, 3=Disputed)
  vehicleNumber?: string;
  driverName?: string;
  deliveryAddress?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  version?: string;
}

export interface WaybillFilterDto {
  waybillDateFrom?: string;
  waybillDateTo?: string;
  deliveryDateFrom?: string;
  deliveryDateTo?: string;
  status?: WaybillStatus;
  projectId?: string;
  supplierId?: string;
  productCode?: string;
  searchText?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface StatusSummary {
  count: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface MonthlyBreakdown {
  year: number;
  month: number;
  count: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface TopSupplier {
  supplierId: string;
  supplierName: string;
  deliveryCount: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface ProjectTotal {
  projectId: string;
  projectName: string;
  waybillCount: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface DisputedCancelledStats {
  disputedCount: number;
  cancelledCount: number;
  totalCount: number;
  disputedPercentage: number;
  cancelledPercentage: number;
}

export interface WaybillSummaryDto {
  totalQuantityByStatus: Record<string, number>;
  totalAmountByStatus: Record<string, number>;
  monthlyBreakdown: MonthlySummaryDto[];
  topSuppliers: SupplierSummaryDto[];
  projectTotals: ProjectSummaryDto[];
  disputedCount: number;
  cancelledCount: number;
  disputedPercentage: number;
  cancelledPercentage: number;
}

export interface MonthlySummaryDto {
  year: number;
  month: number;
  totalQuantity: number;
  totalAmount: number;
  deliveryCount: number;
}

export interface SupplierSummaryDto {
  supplierId: string;
  supplierName: string;
  totalQuantity: number;
  totalAmount: number;
  deliveryCount: number;
}

export interface ProjectSummaryDto {
  projectId: string;
  projectName: string;
  totalQuantity: number;
  totalAmount: number;
  waybillCount: number;
}

export interface ImportResult {
  totalRows: number;
  successCount: number;
  errorCount: number;
  errors: ImportError[];
  warnings: string[];
  parsedWaybills: any[];
}

export interface ImportError {
  rowNumber: number;
  field: string | null;
  message: string;
  rowData: string;
}

export interface UpdateWaybillStatusDto {
  status: WaybillStatus;
  notes?: string;
}

export interface UpdateWaybillDto {
  waybillDate?: string;
  deliveryDate?: string;
  projectId?: string;
  supplierId?: string;
  productCode?: string;
  quantity?: number;
  unitPrice?: number;
  vehicleNumber?: string;
  driverName?: string;
  deliveryAddress?: string;
  notes?: string;
  version?: string;
}

export interface SupplierSummaryDto {
  supplierId: string;
  supplierName: string;
  totalDeliveries: number;
  totalQuantity: number;
  totalAmount: number;
  averageQuantityPerDelivery: number;
  statusBreakdown: Record<string, number>;
}

export interface StatusBreakdown {
  status: string;
  count: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface SupplierReport {
  supplierId: string;
  supplierName: string;
  deliveryCount: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface ProjectReport {
  projectId: string;
  projectName: string;
  waybillCount: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface ProductReport {
  productCode: string;
  productName: string;
  count: number;
  totalQuantity: number;
  totalAmount: number;
}

export interface MonthlyReportResult {
  year: number;
  month: number;
  generatedAt: string;
  totalWaybills: number;
  totalQuantity: number;
  totalAmount: number;
  statusBreakdown: StatusBreakdown[];
  topSuppliers: SupplierReport[];
  topProjects: ProjectReport[];
  productBreakdown: ProductReport[];
}

export interface JobStatusDto {
  jobId: string;
  state: string;
  createdAt: string;
  result?: string;
  error?: string;
}

export interface JobIdResponse {
  jobId: string;
}
