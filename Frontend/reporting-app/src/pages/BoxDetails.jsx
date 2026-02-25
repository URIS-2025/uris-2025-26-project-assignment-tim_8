import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import { ArrowLeft, Plus } from 'lucide-react';
import './BoxDetails.css';

// Mock data
const mockSubmissions = [
    { id: 'SUB-101', title: 'Coffee machine in breakroom is broken', author: 'Anonymous', date: 'Oct 24, 2023', status: 'New', priority: 'High' },
    { id: 'SUB-102', title: 'Need better lighting in the parking lot', author: 'Anonymous', date: 'Oct 23, 2023', status: 'In Progress', priority: 'Medium' },
    { id: 'SUB-103', title: 'Suggestion for flexible working hours', author: 'Jane Doe', date: 'Oct 20, 2023', status: 'Reviewing', priority: 'Low' },
    { id: 'SUB-104', title: 'Water leak in 3rd floor bathroom', author: 'Anonymous', date: 'Oct 19, 2023', status: 'Resolved', priority: 'High' },
    { id: 'SUB-105', title: 'Request for new monitor stands', author: 'John Smith', date: 'Oct 15, 2023', status: 'Closed', priority: 'Low' },
];

const BoxDetails = () => {
    const { boxId } = useParams();
    const navigate = useNavigate();
    const [submissions, setSubmissions] = useState(mockSubmissions);

    // Define columns for DataTable
    const columns = [
        { header: 'ID', accessor: 'id', width: '100px' },
        {
            header: 'Title / Subject',
            accessor: 'title',
            render: (row) => <span style={{ fontWeight: 500 }}>{row.title}</span>
        },
        { header: 'Author', accessor: 'author', width: '150px' },
        { header: 'Date', accessor: 'date', width: '120px' },
        {
            header: 'Status',
            accessor: 'status',
            width: '130px',
            render: (row) => <StatusBadge type="status" status={row.status} />
        },
        {
            header: 'Priority',
            accessor: 'priority',
            width: '120px',
            render: (row) => <StatusBadge type="priority" status={row.priority} />
        }
    ];

    const handleRowClick = (row) => {
        // Navigate to individual submission details
        navigate(`/admin/submissions/${row.id}`);
    };

    const handleActionClick = (row) => {
        // Open action menu (e.g. Delete, Assign Priority)
        console.log('Action menu for:', row.id);
    };

    return (
        <div className="box-details-container animate-fade-in">
            <div className="back-link" onClick={() => navigate(-1)}>
                <ArrowLeft size={16} /> Back to Boxes
            </div>

            <div className="page-header">
                <div>
                    <h1 className="page-title">Facilities & Maintenance</h1>
                    <p className="page-description">Problem Box &bull; ID: {boxId || 'BOX-123'} &bull; Tech Corp</p>
                </div>
            </div>

            <div className="box-stats-row">
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">Total Submissions</span>
                    <span className="mini-stat-value">124</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">New / Unread</span>
                    <span className="mini-stat-value" style={{ color: 'var(--accent-primary)' }}>12</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">High Priority</span>
                    <span className="mini-stat-value" style={{ color: 'var(--danger)' }}>3</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">Resolved</span>
                    <span className="mini-stat-value" style={{ color: 'var(--success)' }}>89</span>
                </div>
            </div>

            <div className="table-wrapper">
                <DataTable
                    title="All Submissions"
                    data={submissions}
                    columns={columns}
                    onRowClick={handleRowClick}
                    onActionClick={handleActionClick}
                    searchPlaceholder="Search by ID, title, or author..."
                    showExport={true}
                />
            </div>
        </div>
    );
};

export default BoxDetails;
