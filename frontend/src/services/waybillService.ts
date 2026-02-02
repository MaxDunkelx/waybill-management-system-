import apiClient from './api';
import type {
  WaybillDto,
  WaybillListDto,
  WaybillFilterDto,
  PagedResult,
  WaybillSummaryDto,
  UpdateWaybillStatusDto,
  UpdateWaybillDto,
} from '../types/api';

export const waybillService = {
  // Get paginated list of waybills
  async getWaybills(filter: WaybillFilterDto): Promise<PagedResult<WaybillListDto>> {
    const params = new URLSearchParams();
    if (filter.waybillDateFrom) params.append('dateFrom', filter.waybillDateFrom);
    if (filter.waybillDateTo) params.append('dateTo', filter.waybillDateTo);
    if (filter.deliveryDateFrom) params.append('deliveryDateFrom', filter.deliveryDateFrom);
    if (filter.deliveryDateTo) params.append('deliveryDateTo', filter.deliveryDateTo);
    if (filter.status) params.append('status', filter.status);
    if (filter.projectId) params.append('projectId', filter.projectId);
    if (filter.supplierId) params.append('supplierId', filter.supplierId);
    if (filter.productCode) params.append('productCode', filter.productCode);
    if (filter.searchText) params.append('searchText', filter.searchText);
    if (filter.page) params.append('pageNumber', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());

    const response = await apiClient.get(`/api/Waybills?${params.toString()}`);
    return response.data;
  },

  // Get single waybill by ID
  async getWaybill(id: string): Promise<WaybillDto> {
    const response = await apiClient.get(`/api/Waybills/${id}`);
    return response.data;
  },

  // Get waybills by project
  async getWaybillsByProject(projectId: string): Promise<WaybillDto[]> {
    const response = await apiClient.get(`/api/Waybills/projects/${projectId}/waybills`);
    return response.data;
  },

  // Get waybill summary
  async getSummary(dateFrom?: string, dateTo?: string): Promise<WaybillSummaryDto> {
    const params = new URLSearchParams();
    if (dateFrom) params.append('dateFrom', dateFrom);
    if (dateTo) params.append('dateTo', dateTo);

    const response = await apiClient.get(`/api/Waybills/summary?${params.toString()}`);
    return response.data;
  },

  // Update waybill status
  async updateStatus(id: string, dto: UpdateWaybillStatusDto): Promise<WaybillDto> {
    const response = await apiClient.patch(`/api/Waybills/${id}/status`, dto);
    return response.data;
  },

  // Update waybill
  async updateWaybill(id: string, dto: UpdateWaybillDto): Promise<WaybillDto> {
    const response = await apiClient.put(`/api/Waybills/${id}`, dto);
    return response.data;
  },
};
