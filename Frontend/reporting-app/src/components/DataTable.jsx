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
    currentPage = 1,
    onPageChange,
    pageSize = 5
}) => {
    // Pagination logic
    const totalItems = data.length;
    const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
    const isPaginated = !!onPageChange;
    const safePage = Math.min(currentPage, totalPages);

    const displayData = isPaginated
        ? data.slice((safePage - 1) * pageSize, safePage * pageSize)
        : data;

    const startEntry = totalItems === 0 ? 0 : (safePage - 1) * pageSize + 1;
    const endEntry = isPaginated ? Math.min(safePage * pageSize, totalItems) : totalItems;

    // Generate page numbers to display
    const getPageNumbers = () => {
        const pages = [];
        const maxVisible = 5;
        let start = Math.max(1, safePage - Math.floor(maxVisible / 2));
        let end = Math.min(totalPages, start + maxVisible - 1);
        if (end - start + 1 < maxVisible) {
            start = Math.max(1, end - maxVisible + 1);
        }
        for (let i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    };

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
                        {displayData.length > 0 ? (
                            displayData.map((row, rowIndex) => (
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
                <span className="pagination-info">
                    Showing {startEntry} to {endEntry} of {totalItems} entries
                </span>
                <div className="pagination-controls">
                    <button
                        className="btn btn-ghost icon-btn small"
                        disabled={safePage <= 1}
                        onClick={() => isPaginated && onPageChange(safePage - 1)}
                    >
                        <ChevronLeft size={16} />
                    </button>
                    {isPaginated ? (
                        getPageNumbers().map((page) => (
                            <span
                                key={page}
                                className={`page-number ${page === safePage ? 'active' : ''}`}
                                onClick={() => onPageChange(page)}
                                style={{ cursor: 'pointer' }}
                            >
                                {page}
                            </span>
                        ))
                    ) : (
                        <span className="page-number active">1</span>
                    )}
                    <button
                        className="btn btn-ghost icon-btn small"
                        disabled={safePage >= totalPages}
                        onClick={() => isPaginated && onPageChange(safePage + 1)}
                    >
                        <ChevronRight size={16} />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default DataTable;
