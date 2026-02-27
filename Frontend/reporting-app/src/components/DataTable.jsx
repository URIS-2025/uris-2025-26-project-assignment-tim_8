import React from 'react';
import { Search, Filter, Download, ChevronLeft, ChevronRight, MoreVertical } from 'lucide-react';
import './DataTable.css';

const DataTable = ({
    title,
    data,
    columns,
    onRowClick,
    onActionClick,
    searchPlaceholder = 'Search...',
    showExport = false
}) => {
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
                        {data.length > 0 ? (
                            data.map((row, rowIndex) => (
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

            {/* Pagination (Static UI for now) */}
            <div className="table-pagination">
                <span className="pagination-info">Showing 1 to {Math.min(data.length, 10)} of {data.length} entries</span>
                <div className="pagination-controls">
                    <button className="btn btn-ghost icon-btn small" disabled>
                        <ChevronLeft size={16} />
                    </button>
                    <span className="page-number active">1</span>
                    <span className="page-number">2</span>
                    <span className="page-number">3</span>
                    <button className="btn btn-ghost icon-btn small">
                        <ChevronRight size={16} />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default DataTable;
