import React, { useState, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, LineChart, Line } from 'recharts';
import { Clock, FileText, GitBranch, ChevronDown, ChevronUp, BarChart2 } from 'lucide-react';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';

interface MetricCard {
    title: string;
    value: string | number;
    change?: number;
    icon?: React.ReactNode;
}

// Demonstration Data
const generateData = () => {
    const now = new Date();
    return Array.from({ length: 7 }).map((_, i) => {
        const date = new Date(now);
        date.setDate(date.getDate() - i);
        return {
            date: date.toLocaleDateString(),
            commits: Math.floor(Math.random() * 15),
            files: Math.floor(Math.random() * 5),
            linesChanged: Math.floor(Math.random() * 200)
        };
    }).reverse();
};

const MetricCard: React.FC<MetricCard> = ({ title, value, change, icon }) => {
    const isPositive = change && change > 0;
    const isNegative = change && change < 0;
    
    return (
        <div className="bg-gray-800 rounded-lg p-4 flex flex-col gap-2">
            <div className="flex justify-between items-center">
                <h3 className="text-gray-400 text-sm">{title}</h3>
                {icon && <div className="text-gray-400">{icon}</div>}
            </div>
            <div className="flex justify-between items-end">
                <span className="text-2xl font-bold text-white">{value}</span>
                {change !== undefined && (
                    <div className={`flex items-center ${isPositive ? 'text-green-500' : isNegative ? 'text-red-500' : 'text-gray-400'}`}>
                        {isPositive ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                        <span className="text-sm">{Math.abs(change)}%</span>
                    </div>
                )}
            </div>
        </div>
    );
};

const ProjectMetricsDashboard: React.FC = () => {
    const [activityData, setActivityData] = useState<any[]>([]);
    const [refreshKey, setRefreshKey] = useState(0);

    useEffect(() => {
        setActivityData(generateData());
    }, [refreshKey]);

    const metrics = [
        { 
            title: "Total Commits",
            value: activityData.reduce((sum, day) => sum + day.commits, 0),
            change: 12,
            icon: <GitBranch size={20} />
        },
        {
            title: "Files Changed",
            value: activityData.reduce((sum, day) => sum + day.files, 0),
            change: -5,
            icon: <FileText size={20} />
        },
        {
            title: "Active Time",
            value: "32h 15m",
            change: 8,
            icon: <Clock size={20} />
        },
        {
            title: "Lines Modified",
            value: activityData.reduce((sum, day) => sum + day.linesChanged, 0),
            change: 15,
            icon: <BarChart2 size={20} />
        }
    ];

    return (
        <div className="p-6 flex flex-col gap-6">
            {/* Header */}
            <div className="flex justify-between items-center">
                <h1 className="text-2xl font-bold text-white">Project Metrics Dashboard</h1>
                <button 
                    onClick={() => setRefreshKey(k => k + 1)}
                    className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md"
                >
                    Refresh Data
                </button>
            </div>

            {/* Info Alert */}
            <Alert>
                <AlertTitle>Dashboard Demo</AlertTitle>
                <AlertDescription>
                    This is a sample dashboard demonstrating React components with Recharts integration.
                    Data is randomly generated. Click Refresh to see different values.
                </AlertDescription>
            </Alert>

            {/* Metric Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                {metrics.map((metric, index) => (
                    <MetricCard key={index} {...metric} />
                ))}
            </div>

            {/* Charts */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Commit Activity */}
                <div className="bg-gray-800 p-4 rounded-lg">
                    <h2 className="text-lg font-semibold text-white mb-4">Commit Activity</h2>
                    <BarChart width={500} height={300} data={activityData}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="date" />
                        <YAxis />
                        <Tooltip />
                        <Legend />
                        <Bar dataKey="commits" fill="#3B82F6" />
                    </BarChart>
                </div>

                {/* Lines Changed */}
                <div className="bg-gray-800 p-4 rounded-lg">
                    <h2 className="text-lg font-semibold text-white mb-4">Lines Modified</h2>
                    <LineChart width={500} height={300} data={activityData}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="date" />
                        <YAxis />
                        <Tooltip />
                        <Legend />
                        <Line type="monotone" dataKey="linesChanged" stroke="#10B981" />
                    </LineChart>
                </div>
            </div>
        </div>
    );
};

export default ProjectMetricsDashboard;