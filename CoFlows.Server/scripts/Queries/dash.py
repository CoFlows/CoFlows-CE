'''
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 '''
 
import dash
import dash_table
from dash.dependencies import Input, Output, State
from dash.exceptions import PreventUpdate
import dash_core_components as dcc
import dash_html_components as html
import plotly.graph_objs as go
import pathlib
import requests
from flask import request
import time
import threading
import logging
import json

import QuantApp.Kernel as qak

# This app can be access through
# http://localhost/dash/$WID$/XXX.py?uid={User Secret}

# Plotly/Dash code as required by CoFlows
dash_init = True
__assetsFolder = '/app/mnt/Files/assets'

# Function to get arguments from URL
def getArgs(_args):
    args = request.headers['Referer']
    if (not args is None) and args.find('?') >= 0:
        args = args[args.find('?') + 1:]
        args = args.split('&')
        args = { element[0]: element[1] for element in map(lambda x: x.split('='), args) }
        return args
    return None

def run(port, path):

    global dash_init, __assetsFolder
    if dash_init:
        dash_init = False

        def inner():

            log = logging.getLogger('werkzeug')
            log.setLevel(logging.ERROR)
 
            # shutdown existing dash 
            try:
                requests.get(url = 'http://localhost:' + str(port) + path + 'shutdown')
                time.sleep(5)
                # print('done waiting...')
            except: 
                pass

            app = dash.Dash(
               __name__, 
               meta_tags=[{"name": "viewport", "content": "width=device-width"}], 
               url_base_pathname = path,
               assets_folder=__assetsFolder
            )
            app.url_base_pathname = path
            
            # ALL DASH CODE MUST START HERE
            # USER: Start Layout Define
            app.layout = html.Div(
                children=[
                    # Leave this section here. It used to access the
                    # URL and store the sessions permission and secret information
                    # Start -----
                    dcc.Location(id='url', refresh=False),
                    html.Div(
                        children= [
                            html.Div(id='link_id', children=[]),
                            html.Div(id='perm_id', style=dict(display='none')),
                            html.Div(id='secret', style=dict(display='none')),
                        ],
                    ),
                    # Finish -----

                    # Main layout section. Please edit this here to customise
                    html.Div(
                        id='main_id',
                        style=dict(display='none'),
                        className='row',
                        children=[
                            html.Div(
                                id='title_div',
                                children= [
                                    html.H3(id='title', children='...'),
                                ],
                            ),

                            html.Div(
                                className='row',
                                style=dict(width='98.5%'),
                                children=[
                                    html.Div(
                                        style=dict(width='98.5%'),
                                        className='row',
                                        id='timeseries_chart_output_div',
                                        children=[
                                            html.Br(),
                                            html.Div(
                                                className='row',
                                                children=[
                                                    html.Div(
                                                        className='twelve columns',
                                                        children=[
                                                            dcc.Graph(
                                                                id='timeseries_chart_output',
                                                            )
                                                        ]
                                                    )
                                                ]
                                            )
                                        ]
                                    ),
                                ]
                            )
                        ]
                    ),

                    # Layout that is shown while the app is loading
                    html.Div(
                        id='loading_id',
                        style=dict(display='none'),
                        className='row',
                        children=[
                            html.Div(
                                children= [
                                    html.H3(children='CoFlows App'),
                                    html.H4(children='Loading....'),
                                ]
                            )
                        ]
                    ),

                    # Layout that is shown when access to the group is denied
                    html.Div(
                        id='denied_id',
                        style=dict(display='none'),
                        className='row',
                        children=[
                            html.Div(
                                children= [
                                    html.H3(children='CoFlows App'),
                                    html.H4(children='Permission denied....'),
                                ]
                            )
                        ]
                    )
                ]
            )

            # Sets related permission, user secret and check permissions from URL
            @app.callback(
                [
                    Output('perm_id', 'children'),
                    Output('secret', 'children'),
                    Output('main_id', 'style'),
                    Output('denied_id', 'style'),
                    Output('loading_id', 'style'),
                ],
                [
                    Input('url','search')
                ])
            def set_start_values(args):
                args = getArgs(args)
                if args is not None:
                    uid = args['uid']

                    cuser = qak.User.ContextUserBySecret(uid)
                    qgroup = qak.Group.FindGroup('$WID$')
                    perm = qgroup.PermissionSecret(uid)
                    
                    if perm > -1:
                        return [perm, uid, dict(), dict(display='none'), dict(display='none')]
                    elif perm == -1:
                        return [perm, uid, dict(display='none'), dict(), dict(display='none')]
                    else:
                        return [perm, uid, dict(display='none'), dict(display='none'), dict()]
                    
                return [-1, '', dict(display='none'), dict(), dict(display='none')]

            # Sample function that generates timeseries
            # Please edit this to customise your logic
            @app.callback(
                [
                    Output('title', 'children'),
                    Output('timeseries_chart_output', 'figure')
                ],
                [
                    Input('secret', 'children'),
                    Input('perm_id', 'children')
                ]
            )
            def set_timeseries_chart(secret, perm_id):

                charts = []
                yaxis_type = 'Linear'

                cuser = qak.User.ContextUserBySecret(secret)
                    
                x_axis = [0, 1, 2, 3, 4]
                y_axis = [0, 1, 2, 3, 4]

                charts.append(dict(
                        name='line 0',
                        x = x_axis,
                        y = y_axis,
                        mode='lines',
                    ))

                if perm_id > -1:
                    return [
                        str(cuser.FirstName) + ' has permission ' + str(perm_id),
                        dict(
                            data = charts,
                            layout = dict(
                                yaxis={
                                    'title': 'Y Axis',
                                    'type': 'linear' if yaxis_type == 'Linear' else 'log',
                                    
                                },
                                margin={'l': 40, 'b': 30, 't': 10, 'r': 0},
                                hovermode='closest',
                                height=800,
                                legend = dict(
                                    orientation = 'h'
                                )
                            )
                        )
                    ]
                else:
                    return [ '',dict() ]


            # ALL DASH CODE MUST END HERE


            # necessary to shutdown server incase the code change
            @app.server.route(path + 'shutdown', methods=['GET'])
            def shutdown():
                try:
                    # Place to delete all variables from memory
                    pass
                except Exception as e: 
                    pass
 
                func = request.environ.get('werkzeug.server.shutdown')
                if func is None:
                    raise RuntimeError('Not running with the Werkzeug Server')
                func()

            return app.run_server(port=port, debug=False, threaded=True)

        
        server = threading.Thread(target = inner)
        server.start() 
 



