/**
 * Force-directed graph visualization using D3.js
 * Creates an interactive network visualization for Cinemarco's relationship graph
 */

import * as d3 from 'd3';

// Store the simulation instance for external control
let simulation = null;
let svg = null;
let container = null;
let zoom = null;

// Color palette for node types
const nodeColors = {
    movie: '#c9a227',      // Gold/amber - matches the app's accent
    series: '#9333ea',     // Purple
    friend: '#22c55e',     // Green
    contributor: '#0ea5e9', // Sky blue
    collection: '#f43f5e'  // Rose
};

// Node sizes by type
const nodeSizes = {
    movie: 30,
    series: 30,
    friend: 24,
    contributor: 24,
    collection: 26
};

/**
 * Initialize the force-directed graph
 * @param {string} containerId - The ID of the container element
 * @param {Object} graphData - The graph data with nodes and edges
 * @param {Function} onNodeSelect - Callback when a node is selected (single click)
 * @param {Function} onNodeFocus - Callback when a node is focused (double click)
 */
export function initializeGraph(containerId, graphData, onNodeSelect, onNodeFocus) {
    console.log("initializeGraph called with:", { containerId, graphData, onNodeSelect });
    const containerEl = document.getElementById(containerId);
    if (!containerEl) return;

    // Clear existing graph
    d3.select(`#${containerId}`).selectAll('*').remove();

    const width = containerEl.clientWidth;
    const height = containerEl.clientHeight || 600;

    // Transform the graph data into D3-friendly format
    const nodes = transformNodes(graphData.Nodes);
    const links = transformLinks(graphData.Edges, nodes);
    console.log("Transformed nodes:", nodes.length, nodes);
    console.log("Transformed links:", links.length, links);

    // Create SVG
    svg = d3.select(`#${containerId}`)
        .append('svg')
        .attr('width', '100%')
        .attr('height', '100%')
        .attr('viewBox', [0, 0, width, height])
        .attr('class', 'graph-svg');

    // Add gradient definitions for nodes
    addGradients(svg);

    // Create a container for zoomable content
    container = svg.append('g').attr('class', 'graph-container');

    // Add zoom behavior
    zoom = d3.zoom()
        .scaleExtent([0.1, 4])
        .on('zoom', (event) => {
            container.attr('transform', event.transform);
        });

    svg.call(zoom);

    // Create the simulation
    simulation = d3.forceSimulation(nodes)
        .force('link', d3.forceLink(links).id(d => d.id).distance(100).strength(0.5))
        .force('charge', d3.forceManyBody().strength(-300))
        .force('center', d3.forceCenter(width / 2, height / 2))
        .force('collision', d3.forceCollide().radius(d => nodeSizes[d.type] + 10));

    // Create links (edges)
    const link = container.append('g')
        .attr('class', 'links')
        .selectAll('line')
        .data(links)
        .join('line')
        .attr('stroke', 'rgba(255, 255, 255, 0.2)')
        .attr('stroke-width', 1.5)
        .attr('stroke-dasharray', d => d.relationship === 'WatchedWith' ? 'none' : '4,4');

    // Create node groups
    const node = container.append('g')
        .attr('class', 'nodes')
        .selectAll('g')
        .data(nodes)
        .join('g')
        .attr('class', 'node')
        .call(drag(simulation))
        .on('click', (event, d) => {
            event.stopPropagation();
            if (onNodeSelect) {
                onNodeSelect(d);
            }
        })
        .on('dblclick', (event, d) => {
            event.stopPropagation();
            if (onNodeFocus) {
                onNodeFocus(d);
            }
        });

    // Add circles for circular nodes (friend, contributor, collection)
    node.filter(d => d.type === 'friend' || d.type === 'contributor' || d.type === 'collection')
        .append('circle')
        .attr('r', d => nodeSizes[d.type])
        .attr('fill', d => `url(#gradient-${d.type})`)
        .attr('stroke', d => nodeColors[d.type])
        .attr('stroke-width', 2)
        .attr('class', 'node-circle');

    // Add text for circular nodes
    node.filter(d => d.type === 'friend' || d.type === 'contributor' || d.type === 'collection')
        .append('text')
        .attr('text-anchor', 'middle')
        .attr('dy', '0.35em')
        .attr('fill', 'white')
        .attr('font-size', '10px')
        .attr('font-weight', 'bold')
        .text(d => getInitials(d.name));

    // Add rectangles for movie/series nodes (poster-like)
    node.filter(d => d.type === 'movie' || d.type === 'series')
        .append('rect')
        .attr('width', 40)
        .attr('height', 60)
        .attr('x', -20)
        .attr('y', -30)
        .attr('rx', 4)
        .attr('fill', d => d.posterPath ? 'transparent' : `url(#gradient-${d.type})`)
        .attr('stroke', d => nodeColors[d.type])
        .attr('stroke-width', 2)
        .attr('class', 'node-poster');

    // Add poster images for movie/series nodes
    node.filter(d => (d.type === 'movie' || d.type === 'series') && d.posterPath)
        .append('image')
        .attr('href', d => `/images/posters${d.posterPath}`)
        .attr('width', 40)
        .attr('height', 60)
        .attr('x', -20)
        .attr('y', -30)
        .attr('preserveAspectRatio', 'xMidYMid slice')
        .attr('clip-path', 'inset(0 round 4px)');

    // Add labels below nodes
    node.append('text')
        .attr('text-anchor', 'middle')
        .attr('dy', d => (d.type === 'movie' || d.type === 'series') ? 45 : 40)
        .attr('fill', 'rgba(255, 255, 255, 0.8)')
        .attr('font-size', '10px')
        .text(d => truncate(d.name, 12));

    // Add hover effects
    node.on('mouseenter', function(event, d) {
        d3.select(this).select('.node-circle, .node-poster')
            .transition()
            .duration(200)
            .attr('stroke-width', 4);

        // Highlight connected links
        link.attr('stroke', l =>
            l.source.id === d.id || l.target.id === d.id
                ? 'rgba(255, 255, 255, 0.6)'
                : 'rgba(255, 255, 255, 0.1)'
        );
    })
    .on('mouseleave', function() {
        d3.select(this).select('.node-circle, .node-poster')
            .transition()
            .duration(200)
            .attr('stroke-width', 2);

        link.attr('stroke', 'rgba(255, 255, 255, 0.2)');
    });

    // Click on background to deselect
    svg.on('click', () => {
        if (onNodeSelect) {
            onNodeSelect(null);
        }
    });

    // Update positions on simulation tick
    simulation.on('tick', () => {
        link
            .attr('x1', d => d.source.x)
            .attr('y1', d => d.source.y)
            .attr('x2', d => d.target.x)
            .attr('y2', d => d.target.y);

        node.attr('transform', d => `translate(${d.x}, ${d.y})`);
    });

    return simulation;
}

/**
 * Transform graph nodes from API format to D3 format
 */
function transformNodes(apiNodes) {
    return apiNodes.map((node, index) => {
        // Node format: MovieNode (EntryId, title, posterPath option) etc.
        if (node.Case === 'MovieNode') {
            const [entryId, title, posterPath] = node.Fields;
            return {
                id: `movie-${entryId.Fields[0]}`,
                type: 'movie',
                entryId: entryId.Fields[0],
                name: title,
                posterPath: posterPath
            };
        } else if (node.Case === 'SeriesNode') {
            const [entryId, name, posterPath] = node.Fields;
            return {
                id: `series-${entryId.Fields[0]}`,
                type: 'series',
                entryId: entryId.Fields[0],
                name: name,
                posterPath: posterPath
            };
        } else if (node.Case === 'FriendNode') {
            const [friendId, name] = node.Fields;
            return {
                id: `friend-${friendId.Fields[0]}`,
                type: 'friend',
                friendId: friendId.Fields[0],
                name: name
            };
        } else if (node.Case === 'ContributorNode') {
            const [contributorId, name, profilePath] = node.Fields;
            return {
                id: `contributor-${contributorId.Fields[0]}`,
                type: 'contributor',
                contributorId: contributorId.Fields[0],
                name: name,
                profilePath: profilePath
            };
        } else if (node.Case === 'CollectionNode') {
            const [collectionId, name] = node.Fields;
            return {
                id: `collection-${collectionId.Fields[0]}`,
                type: 'collection',
                collectionId: collectionId.Fields[0],
                name: name
            };
        }
        return null;
    }).filter(n => n !== null);
}

/**
 * Transform graph edges from API format to D3 format
 */
function transformLinks(apiEdges, nodes) {
    const nodeMap = new Map(nodes.map(n => [n.id, n]));

    return apiEdges.map(edge => {
        const sourceNode = transformNodes([edge.Source])[0];
        const targetNode = transformNodes([edge.Target])[0];

        if (!sourceNode || !targetNode) return null;

        // Only create link if both nodes exist in our node set
        if (!nodeMap.has(sourceNode.id) || !nodeMap.has(targetNode.id)) return null;

        return {
            source: sourceNode.id,
            target: targetNode.id,
            relationship: edge.Relationship.Case || 'Unknown'
        };
    }).filter(l => l !== null);
}

/**
 * Add gradient definitions for node fills
 */
function addGradients(svg) {
    const defs = svg.append('defs');

    Object.entries(nodeColors).forEach(([type, color]) => {
        const gradient = defs.append('radialGradient')
            .attr('id', `gradient-${type}`)
            .attr('cx', '50%')
            .attr('cy', '30%')
            .attr('r', '70%');

        gradient.append('stop')
            .attr('offset', '0%')
            .attr('stop-color', d3.color(color).brighter(0.5));

        gradient.append('stop')
            .attr('offset', '100%')
            .attr('stop-color', d3.color(color).darker(0.3));
    });
}

/**
 * Create drag behavior for nodes
 */
function drag(simulation) {
    function dragstarted(event) {
        if (!event.active) simulation.alphaTarget(0.3).restart();
        event.subject.fx = event.subject.x;
        event.subject.fy = event.subject.y;
    }

    function dragged(event) {
        event.subject.fx = event.x;
        event.subject.fy = event.y;
    }

    function dragended(event) {
        if (!event.active) simulation.alphaTarget(0);
        event.subject.fx = null;
        event.subject.fy = null;
    }

    return d3.drag()
        .on('start', dragstarted)
        .on('drag', dragged)
        .on('end', dragended);
}

/**
 * Get initials from a name
 */
function getInitials(name) {
    return name
        .split(' ')
        .map(word => word[0])
        .join('')
        .substring(0, 2)
        .toUpperCase();
}

/**
 * Truncate text with ellipsis
 */
function truncate(text, maxLength) {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength - 1) + '...';
}

/**
 * Update the graph zoom level
 */
export function setZoom(level) {
    if (svg && zoom) {
        svg.transition()
            .duration(300)
            .call(zoom.scaleTo, level);
    }
}

/**
 * Reset zoom to fit all nodes
 */
export function resetZoom() {
    if (svg && zoom && container) {
        const bounds = container.node().getBBox();
        const parent = svg.node().parentElement;
        const fullWidth = parent.clientWidth;
        const fullHeight = parent.clientHeight;

        const midX = bounds.x + bounds.width / 2;
        const midY = bounds.y + bounds.height / 2;
        const scale = 0.8 / Math.max(bounds.width / fullWidth, bounds.height / fullHeight);

        svg.transition()
            .duration(500)
            .call(zoom.transform, d3.zoomIdentity
                .translate(fullWidth / 2, fullHeight / 2)
                .scale(scale)
                .translate(-midX, -midY));
    }
}

/**
 * Focus on a specific node
 */
export function focusOnNode(nodeId) {
    if (svg && zoom && container && simulation) {
        const node = simulation.nodes().find(n => n.id === nodeId);
        if (node) {
            const parent = svg.node().parentElement;
            const fullWidth = parent.clientWidth;
            const fullHeight = parent.clientHeight;

            svg.transition()
                .duration(500)
                .call(zoom.transform, d3.zoomIdentity
                    .translate(fullWidth / 2, fullHeight / 2)
                    .scale(2)
                    .translate(-node.x, -node.y));
        }
    }
}

/**
 * Clean up the graph
 */
export function destroyGraph(containerId) {
    if (simulation) {
        simulation.stop();
        simulation = null;
    }
    d3.select(`#${containerId}`).selectAll('*').remove();
    svg = null;
    container = null;
    zoom = null;
}
