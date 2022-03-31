import { Component, OnInit } from "@angular/core";
import { Observable } from "rxjs";
import { Subreddit } from "../services/subreddit.service";

@Component({
  selector: "app-subreddit-list",
  template: `
    <mat-grid-list cols="12" rowHeight="3:1">
      <mat-grid-tile *ngFor="let subreddit of subreddit.subreddits">{{ subreddit }}</mat-grid-tile>
    </mat-grid-list>
   `,
  styles: [],
})
export class SubredditListComponent implements OnInit {
  subreddits!: Observable<string[]>;

  constructor(public subreddit: Subreddit) {}

  ngOnInit(): void {
    
    this.subreddit.loadSubreddits().subscribe(result => {
      // get the results
      this.subreddit.subreddits = result
      // sort them alphabetically
      this.subreddit.subreddits.sort(function(a, b) {
        var textA = a.toUpperCase();
        var textB = b.toUpperCase();
        return (textA < textB) ? -1 : (textA > textB) ? 1 : 0;
    });
    });
  }
}