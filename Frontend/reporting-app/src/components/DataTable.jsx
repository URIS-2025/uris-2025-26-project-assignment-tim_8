import React, { useState, useMemo } from 'react';
import { Search, Filter, Download, ChevronLeft, ChevronRight, MoreVertical } from 'lucide-react';
import './DataTable.css';

const DataTable = ({
    title,
    data,
    columns,
    onRowClick,
    onActionClick,
    searchPlaceholder = 'Search...',
    showExport = false,
    searchValue = '',
    onSearchChange,
    itemsPerPage = 10
}) => {
    const [currentPage, setCurrentPage] = useState(0);

    // Reset to first page when data or search changes
    const dataLength = data.length;
    const [prevDataLength, setPrevDataLength] = useState(dataLength);
    const [prevSearchValue, setPrevSearchValue] = useState(searchValue);

    if (searchValue !== prevSearchValue) {
        setPrevSearchValue(searchValue);
        setCurrentPage(0);
    }
    if (dataLength !== prevDataLength) {
        setPrevDataLength(dataLength);
        if (currentPage > 0 && currentPage >= Math.ceil(dataLength / itemsPerPage)) {
            setCurrentPage(0);
        }
    }

    const totalPages = Math.max(1, Math.ceil(data.length / itemsPerPage));
    const paginatedData = data.slice(currentPage * itemsPerPage, (currentPage + 1) * itemsPerPage);
    const startEntry = data.length === 0 ? 0 : currentPage * itemsPerPage + 1;
    const endEntry = Math.min((currentPage + 1) * itemsPerPage, data.length);

    // Build page numbers to display (max 5 visible)
    const pageNumbers = useMemo(() => {
        const pages = [];
        let start = Math.max(0, currentPage - 2);
        let end = Math.min(totalPages - 1, start + 4);
        start = Math.max(0, end - 4);
        for (let i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    }, [currentPage, totalPages]);

    return (
        <div className="data-table-container glass-panel animate-fade-in">
            {/* Table Header Controls */}
            <div className="table-header-controls">
                <h2 className="table-title">{title}</h2>

                <div className="table-actions">
                    <div className="search-wrapper">
                        <Search size={18} className="search-icon" />
                        <input
                            type="text"
                            placeholder={searchPlaceholder}
                            className="table-search-input"
                            value={searchValue}
                            onChange={(e) => onSearchChange && onSearchChange(e.target.value)}
                        />
                    </div>
                    <button className="btn btn-ghost icon-btn" title="Filter">
                        <Filter size={18} />
                    </button>
                    {showExport && (
                        <button className="btn btn-ghost icon-btn" title="Export Data">
                            <Download size={18} />
                        </button>
                    )}
                </div>
            </div>

            {/* Table Data */}
            <div className="table-responsive">
                <table className="custom-table">
                    <thead>
                        <tr>
                            {columns.map((col, index) => (
                                <th key={index} style={{ width: col.width || 'auto', textAlign: col.align || 'left' }}>
                                    {col.header}
                                </th>
                            ))}
                            {onActionClick && <th style={{ width: '60px' }}></th>}
                        </tr>
                    </thead>
                    <tbody>
                        {paginatedData.length > 0 ? (
                            paginatedData.map((row, rowIndex) => (
                                <tr
                                    key={row.id || rowIndex}
                                    onClick={() => onRowClick && onRowClick(row)}
                                    className={onRowClick ? 'clickable-row' : ''}
                                >
                                    {columns.map((col, colIndex) => (
                                        <td key={colIndex} style={{ textAlign: col.align || 'left' }}>
                                            {col.render ? col.render(row) : row[col.accessor]}
                                        </td>
                                    ))}
                                    {onActionClick && (
                                        <td onClick={(e) => e.stopPropagation()}>
                                            <button
                                                className="btn-ghost icon-btn small"
                                                onClick={() => onActionClick(row)}
                                            >
                                                <MoreVertical size={16} />
                                            </button>
                                        </td>
                                    )}
                                </tr>
                            ))
                        ) : (
                            <tr>
                                <td colSpan={columns.length + (onActionClick ? 1 : 0)} className="empty-state">
                                    No data available.
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>

            {/* Pagination */}
            <div className="table-pagination">
                <span className="pagination-info">Showing {startEntry} to {endEntry} of {data.length} entries</span>
                <div className="pagination-controls">
                    <button
                        className="btn btn-ghost icon-btn small"
                        disabled={currentPage === 0}
                        onClick={() => setCurrentPage((p) => Math.max(0, p - 1))}
                    >
                        <ChevronLeft size={16} />
                    </button>
                    {pageNumbers.map((pageNum) => (
                        <span
                            key={pageNum}
                            className={`page-number ${pageNum === currentPage ? 'active' : ''}`}
                            onClick={() => setCurrentPage(pageNum)}
                            style={{ cursor: 'pointer' }}
                        >
                            {pageNum + 1}
                        </span>
                    ))}
                    <button
                        className="btn btn-ghost icon-btn small"
                        disabled={currentPage >= totalPages - 1}
                        onClick={() => setCurrentPage((p) => Math.min(totalPages - 1, p + 1))}
                    >
                        <ChevronRight size={16} />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default DataTable;
