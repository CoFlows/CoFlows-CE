import { Component, ViewChild } from '@angular/core';
import { CoFlowsComponent } from '../../coflows/core/coflows.component';
import { CFQueryComponent } from '../../coflows/query/cfquery.component';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

import { ActivatedRoute } from '@angular/router';

import 'codemirror/mode/mllike/mllike.js';
import 'codemirror/mode/clike/clike.js';
import 'codemirror/mode/python/python.js';
import 'codemirror/mode/vb/vb.js';
import 'codemirror/mode/javascript/javascript.js';


import * as CodeMirror from 'codemirror/lib/codemirror.js';


import { NgbTabset } from '@ng-bootstrap/ng-bootstrap';


@Component({
  selector: 'code-test',
  styles: [`
  :host >>> .CodeMirror {
    height: auto;
  }
  `],
  templateUrl: 'query.component.html'
})
export class QueryComponent {


    selectedWB = this.cfquery.templaceWB

    editorOptionsFs = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-fsharp',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsCs = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-csharp',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsJava = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-java',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsScala = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-scala',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsVb = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-vb',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsPy = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-cython',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsJs = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/javascript',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    workbooks = []

    workflowID = ""

    workflow = {
        Permissions: []
    }

    permission = -1
    permissionSet = false

    constructor(private activatedRoute: ActivatedRoute, public coflows: CoFlowsComponent, private cfquery: CFQueryComponent, private modalService: NgbModal){

        this.activatedRoute.params.subscribe(params => {
            let wid = params['wid'];
            let id = params['id'];

            this.workflowID = wid

            this.coflows.LinkAction(wid,
                data => { //Load

                    this.workflow = data[0].Value
                    // console.log(this.workflow)

                    this.workflow.Permissions.forEach(x => {
                        if(x.ID == this.coflows.quser.User.Email) this.permission = x.Permission
                    })

                    if(this.permission == 2){
                        this.editorOptionsFs.readOnly = false
                        this.editorOptionsCs.readOnly = false
                        this.editorOptionsVb.readOnly = false
                        this.editorOptionsPy.readOnly = false
                        this.editorOptionsJs.readOnly = false
                        this.editorOptionsJava.readOnly = false
                        this.editorOptionsScala.readOnly = false
                    }

                    this.permissionSet = true
                },
                data => { //Add
                },
                data => { //Exchange
                },
                data => { //Remove
                }
            )
            
            this.coflows.LinkAction(wid + '--Queries', 
                data => { //Load
                    this.workbooks = data
                    if(this.workbooks.length > 0){
                        let tid = this.workbooks.findIndex(x => x.Value.ID == id)
                        
                        this.selectedWB = this.workbooks[Math.max(0, tid)].Value
                        this.pyCode = this.selectedWB.Code.startsWith('import clr') || this.selectedWB.Code.startsWith('#py') || this.selectedWB.Name.endsWith('.py')
                        this.csCode = this.selectedWB.Code.startsWith('//cs') || this.selectedWB.Name.endsWith('.cs')
                        this.jsCode = this.selectedWB.Code.startsWith('//js') || this.selectedWB.Name.endsWith('.js')
                        this.javaCode = this.selectedWB.Code.startsWith('//java') || this.selectedWB.Name.endsWith('.java')
                        this.scalaCode = this.selectedWB.Code.startsWith('//scala') || this.selectedWB.Name.endsWith('.scala')
                        this.vbCode = this.selectedWB.Code.startsWith('\'vb') || this.selectedWB.Name.endsWith('.vb')
                    }
                    else{
                        this.selectedWB = JSON.parse(JSON.stringify(this.cfquery.templaceWB))
                        this.selectedWB.WorkflowID = wid
                        this.workbooks.push({Value : this.selectedWB })
                        
                        this.pyCode = this.selectedWB.Code.startsWith('import clr') || this.selectedWB.Code.startsWith('#py') || this.selectedWB.Name.endsWith('.py')
                        this.csCode = this.selectedWB.Code.startsWith('//cs') || this.selectedWB.Name.endsWith('.cs')
                        this.jsCode = this.selectedWB.Code.startsWith('//js') || this.selectedWB.Name.endsWith('.js')
                        this.javaCode = this.selectedWB.Code.startsWith('//java') || this.selectedWB.Name.endsWith('.java')
                        this.scalaCode = this.selectedWB.Code.startsWith('//scala') || this.selectedWB.Name.endsWith('.scala')
                        this.vbCode = this.selectedWB.Code.startsWith('\'vb') || this.selectedWB.Name.endsWith('.vb')
                    }
                    //this.selectedWB = data[0]
                    // console.log(data)
                },
                data => { //Add
                    // console.log(data)
                },
                data => { //Exchange
                    // console.log('EXCHANGE: ', data)
                    let tid = this.workbooks.findIndex(x => x.Value.ID == data.Value.ID)
                    
                    this.workbooks[tid] = data//.Value

                    if(this.selectedWB.ID == data.Value.ID)
                        this.selectedWB = data.Value

                    this.pyCode = this.selectedWB.Code.startsWith('import clr') || this.selectedWB.Code.startsWith('#py') || this.selectedWB.Name.endsWith('.py')
                    this.csCode = this.selectedWB.Code.startsWith('//cs') || this.selectedWB.Name.endsWith('.cs')
                    this.jsCode = this.selectedWB.Code.startsWith('//js') || this.selectedWB.Name.endsWith('.js')
                    this.javaCode = this.selectedWB.Code.startsWith('//java') || this.selectedWB.Name.endsWith('.java')
                    this.scalaCode = this.selectedWB.Code.startsWith('//scala') || this.selectedWB.Name.endsWith('.scala')
                    this.vbCode = this.selectedWB.Code.startsWith('\'vb') || this.selectedWB.Name.endsWith('.vb')

                },
                data => { //Remove
                    let tid = this.workbooks.findIndex(x => x.Value.ID == data.Value.ID)
                        
                    if(tid > -1)
                        this.workbooks.splice(tid,1)

                    if(this.workbooks.length > 0)
                        this.selectedWB = this.workbooks[0].Value
                    else
                        this.selectedWB = JSON.parse(JSON.stringify(this.cfquery.templaceWB))//this.cfquery.templaceWB

                    this.pyCode = this.selectedWB.Code.startsWith('import clr') || this.selectedWB.Code.startsWith('#py') || this.selectedWB.Name.endsWith('.py')
                    this.csCode = this.selectedWB.Code.startsWith('//cs') || this.selectedWB.Name.endsWith('.cs')
                    this.jsCode = this.selectedWB.Code.startsWith('//js') || this.selectedWB.Name.endsWith('.js')
                    this.javaCode = this.selectedWB.Code.startsWith('//java') || this.selectedWB.Name.endsWith('.java')
                    this.scalaCode = this.selectedWB.Code.startsWith('//scala') || this.selectedWB.Name.endsWith('.scala')
                    this.vbCode = this.selectedWB.Code.startsWith('\'vb') || this.selectedWB.Name.endsWith('.vb')
                }
            );
        });

    }

    @ViewChild('tabs')
    private tabs:NgbTabset;

 
    selectedTab = ""
   
    chgName = false


    pyCode = false
    csCode = false
    vbCode = false
    jsCode = false
    javaCode = false
    scalaCode = false

    selectWBFunc(item){
        this.cfquery.resetExecution()
        this.selectedWB = JSON.parse(item)

        this.pyCode = this.selectedWB.Code.startsWith('import clr') || this.selectedWB.Code.startsWith('#py') || this.selectedWB.Name.endsWith('.py')
        this.csCode = this.selectedWB.Code.startsWith('//cs') || this.selectedWB.Name.endsWith('.cs')
        this.vbCode = this.selectedWB.Code.startsWith('\'vb') || this.selectedWB.Name.endsWith('.vb')
        this.javaCode = this.selectedWB.Code.startsWith('//java') || this.selectedWB.Name.endsWith('.java')
        this.scalaCode = this.selectedWB.Code.startsWith('//scala') || this.selectedWB.Name.endsWith('.scala')
    }

    changeName(){
        this.chgName = !this.chgName
    }
     

    modalMessage = ""
    open(content, workbook) {
        this.modalService.open(content).result.then((result) => {
            
            console.log(result)
            if(result == 'delete'){
                this.modalMessage = "Removing..."

                this.cfquery.removeCode(workbook)

                let idx = this.workbooks.findIndex(x => x.Value.ID == workbook.ID)
                
                this.workbooks.splice(idx,1)
                this.selectedWB = this.workbooks[0].Value

                this.modalMessage = ''
                this.modalService.dismissAll(content)
            }

        }, (reason) => {
            console.log(reason)
        });
    }
    // removeCode(workbook){
    //     this.cfquery.removeCode(workbook)

    //     let idx = this.workbooks.findIndex(x => x.Value.ID == workbook.ID)
        
    //     this.workbooks.splice(idx,1)
    //     this.selectedWB = this.workbooks[0].Value
    // }

    newCode(){
        this.selectedWB = JSON.parse(JSON.stringify(this.cfquery.templaceWB))
        this.selectedWB.WorkflowID = this.workflowID
        this.workbooks.push({ Value: this.selectedWB })
        
    }
}